using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Flurl.Http;
using GeoAPI.Geometries;
using HtmlAgilityPack;
using SpbBuildingAgeMaps.DataModel;

namespace SpbBuildingAgeMaps.DataProviders
{
  class ReformaGhkBuidlingsProvider : ObservableBase<BuildingInfoWithLocation>
  {
    private readonly ReplaySubject<BuildingInfoWithLocation> _subject = new ReplaySubject<BuildingInfoWithLocation>();
    private const string SpbBuildingsRootPageUrl = @"https://www.reformagkh.ru/myhouse?tid=2276347";
    private const string SaveCheckDataUrl = @"https://www.reformagkh.ru/save-check-data";
    private int _numberOfLinksLeft = 1;
    private FlurlClient _flurlClient;

    public ReformaGhkBuidlingsProvider()
    {
      _flurlClient = new FlurlClient().EnableCookies().WithHeader("User-Agent", WebHelper.UserAgent);
      CrawlBuildingsAsync(SpbBuildingsRootPageUrl);
    }

    protected override IDisposable SubscribeCore(IObserver<BuildingInfoWithLocation> observer)
    {
      return _subject.Subscribe(observer);
    }

    private static readonly Regex YandexConfigCoords = new Regex(@"center: \[([\d\.]+),([\d\.]+)\]", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private async Task CrawlBuildingsAsync(string url)
    {
      HtmlDocument htmlDocument = await DownloadHtmlPageAsync(url).ConfigureAwait(false);

      if (url.IndexOf("myhouse/profile/view", StringComparison.InvariantCultureIgnoreCase) >= 0)
      {
        await ProcessBuildingInformationPage(htmlDocument).ConfigureAwait(false);
      }
      else
      {
        bool b = await ProcessNeighborhoodListPage(htmlDocument).ConfigureAwait(false)
          || await ProcessBuildingListPage(htmlDocument, url).ConfigureAwait(false);
      }

      if (Interlocked.Decrement(ref _numberOfLinksLeft) <= 0)
      {
        _subject.OnCompleted();
      }
    }

    private async Task<bool> ProcessNeighborhoodListPage(HtmlDocument htmlDocument)
    {
      IEnumerable<string> links =
        (htmlDocument.DocumentNode.SelectNodes("//a[@class='georefs']") ?? Enumerable.Empty<HtmlNode>())
        .Select(node => node.Attributes["href"]?.Value).Where(href => !string.IsNullOrEmpty(href)).ToList();

      if (links.Any())
      {
        foreach (string link in links)
        {
          await AddUrlToQueueAsync("https://www.reformagkh.ru/myhouse" + link).ConfigureAwait(false);
        }
        return true;
      }

      return false;
    }

    private async Task<bool> ProcessBuildingListPage(HtmlDocument htmlDocument, string url)
    {
      List<string> buildingsUrl =
        (htmlDocument.DocumentNode.SelectNodes("//div[@class='grid']//tr/td/a[@href]") ?? Enumerable.Empty<HtmlNode>())
        .Select(node => node.Attributes["href"]?.Value).Where(href =>
          !string.IsNullOrEmpty(href) &&
          href.StartsWith("/myhouse/profile/view/", StringComparison.InvariantCultureIgnoreCase)).ToList();
      if (buildingsUrl.Any())
      {
        foreach (string building in buildingsUrl)
        {
          await AddUrlToQueueAsync("https://www.reformagkh.ru" + building).ConfigureAwait(false);
        }

        Uri uri = new Uri(url);
        NameValueCollection query = HttpUtility.ParseQueryString(uri.Query);

        string pageValue = query.Get("page");
        if (!string.IsNullOrEmpty(pageValue) && int.TryParse(pageValue, out int currentPage))
        {
          await AddUrlToQueueAsync(uri.SetParameter("page", (currentPage + 1).ToString()).ToString()).ConfigureAwait(false);
        }
        else
        {
          await AddUrlToQueueAsync(url + "&sort=name&order=asc&page=2&limit=20").ConfigureAwait(false);
        }
        return true;
      }

      return false;
    }

    private async Task<bool> ProcessBuildingInformationPage(HtmlDocument htmlDocument)
    {
      List<HtmlNode> houseInfoRows = (htmlDocument.DocumentNode.SelectNodes("//section[contains(@class, 'house_info')]//table[@class='col_list']//tr") ?? Enumerable.Empty<HtmlNode>()).ToList();
      if (houseInfoRows.Any())
      {
        if (houseInfoRows.Count % 2 != 0)
        {
          throw new NotImplementedException();
        }

        int buildYear = 0;

        for (var i = 0; i < houseInfoRows.Count; i += 2)
        {
          string propertyName = houseInfoRows[i].InnerText.Trim();
          string propertyValue = houseInfoRows[i + 1].InnerText.Trim();
          if (propertyName.Equals("Год ввода в эксплуатацию", StringComparison.InvariantCultureIgnoreCase))
          {
            if (int.TryParse(propertyValue, out buildYear))
            {
              break;
            }
            else if (propertyValue.Equals("Не заполнено", StringComparison.InvariantCultureIgnoreCase))
            {
              return true;
            }
            else
            {
              throw new NotImplementedException();
            }
          }
        }

        if (buildYear == 0)
        {
          throw new NotImplementedException();
        }

        Match coordsMatch = YandexConfigCoords.Match(htmlDocument.ParsedText);
        if (!coordsMatch.Success || !double.TryParse(coordsMatch.Groups[1].Value, out double xCord) || !double.TryParse(coordsMatch.Groups[2].Value, out double yCord))
        {
          throw new NotImplementedException();
        }

        HtmlNode addressNode = htmlDocument.DocumentNode.SelectSingleNode("//span[@class='float-left loc_name_ohl width650 word-wrap-break-word']");
        if (addressNode == null)
        {
          throw new NotImplementedException();
        }

        string rawAddress = WebUtility.HtmlDecode(addressNode.InnerText.Trim());
        string normalAddress = AddressHelper.NormalizeAddress(rawAddress).First();

        _subject.OnNext(new BuildingInfoWithLocation { Address = normalAddress, BuildYear = buildYear, Coordinate = new Coordinate(xCord, yCord) });
        return true;
      }

      return false;
    }

    private async Task AddUrlToQueueAsync(string url)
    {
      Interlocked.Increment(ref _numberOfLinksLeft);
      await Task.Delay(200).ConfigureAwait(false);
      await CrawlBuildingsAsync(url).ConfigureAwait(false);
    }

    private static readonly Regex checkKeyRegex = new Regex("checkKey = \'([0-9a-fA-F]+)\'", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex key1Regex = new Regex("key1 = \'([0-9a-fA-F]+)\'", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex key2Regex = new Regex("key2 = \'([0-9a-fA-F]+)\'", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex val1Regex = new Regex("val1 = Number\\(\'(\\d+)\'\\)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex val2Regex = new Regex("val2 = Number\\(\'(\\d+)\'\\)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private async Task<HtmlDocument> DownloadHtmlPageAsync(string url)
    {
      Console.WriteLine(url);
      while (true)
      {
        HttpResponseMessage httpResponseMessage = await url.WithClient(_flurlClient).GetAsync().ConfigureAwait(false);
        string content = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (content.StartsWith(@"<script lang=""text/javascript"">"))
        {
          if (!GetSecureKeys(content, out var keys))
          {
            continue;
          }
          string secureResponse = $"key1={keys.key1}&key2={keys.key2}&check-key={keys.checkKey}&check-value={keys.val1 + keys.val2}";
          HttpResponseMessage responseMessage = await SaveCheckDataUrl.WithClient(_flurlClient)
            .WithHeader("X-Requested-With", "XMLHttpRequest")
            .WithHeader("Content-Type", "application/x-www-form-urlencoded")
            .PostAsync(new StringContent(secureResponse)).ConfigureAwait(false);
          string responseContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
          continue;
        }

        HtmlDocument htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(content);

        return htmlDocument;
      }
    }

    private static bool GetSecureKeys(string jsContent, out (string checkKey, string key1, string key2, ulong val1, ulong val2) keys)
    {
      var key1Match = key1Regex.Match(jsContent);
      var key2Match = key2Regex.Match(jsContent);
      var val1Match = val1Regex.Match(jsContent);
      var val2Match = val2Regex.Match(jsContent);
      var checkKeyMatch = checkKeyRegex.Match(jsContent);

      if (key1Match.Success && key2Match.Success && val1Match.Success && val2Match.Success && checkKeyMatch.Success
        && ulong.TryParse(val1Match.Groups[1].Value, out ulong val1) && ulong.TryParse(val2Match.Groups[1].Value, out ulong val2))
      {
        keys = (checkKeyMatch.Groups[1].Value, key1Match.Groups[1].Value, key2Match.Groups[1].Value, val1, val2);
        return true;
      }
      keys = default;
      return false;
    }
  }
}
