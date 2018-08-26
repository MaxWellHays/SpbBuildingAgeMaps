using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using HtmlAgilityPack;
using SpbBuildingAgeMaps.DataModel;

namespace SpbBuildingAgeMaps.DataProviders
{
  class ReformaGhkBuidlingsProvider : ObservableBase<BuildingInfoWithLocation>
  {
    private readonly ReplaySubject<BuildingInfoWithLocation> _subject = new ReplaySubject<BuildingInfoWithLocation>();
    private const string SpbBuildingsRootPageUrl = @"https://www.reformagkh.ru/myhouse?tid=2276347";
    private const string SaveCheckDataUrl = @"https://www.reformagkh.ru/save-check-data";
    private int _numberOfLinksLeft = 0;
    private FlurlClient _flurlClient;

    public ReformaGhkBuidlingsProvider() {
      _flurlClient = new FlurlClient().EnableCookies().WithHeader("User-Agent", WebHelper.UserAgent);
      CrawlBuildingsAsync(SpbBuildingsRootPageUrl);
    }

    protected override IDisposable SubscribeCore(IObserver<BuildingInfoWithLocation> observer)
    {
      return _subject.Subscribe(observer);
    }

    private async Task CrawlBuildingsAsync(string url) {
      HtmlDocument htmlDocument = await DownloadHtmlPageAsync(url).ConfigureAwait(false);
      IEnumerable<string> links =
        (htmlDocument.DocumentNode.SelectNodes("//a[@class='georefs']") ?? Enumerable.Empty<HtmlNode>())
        .Select(node => node.Attributes["href"]?.Value).Where(href => !string.IsNullOrEmpty(href)).ToList();

      foreach (string link in links)
      {
        await AddUrlToQueueAsync("https://www.reformagkh.ru/myhouse" + link).ConfigureAwait(false);
        return;
      }

      HtmlNode grid = htmlDocument.DocumentNode.SelectSingleNode("//div[@class='grid']");
      if (grid != null)
      {
        List<string> buildingsUrl = (grid.SelectNodes(".//tr/td/a[@href]") ?? Enumerable.Empty<HtmlNode>())
          .Select(node => node.Attributes["href"]?.Value).Where(href => !string.IsNullOrEmpty(href)).ToList();
        foreach (string building in buildingsUrl)
        {
          await AddUrlToQueueAsync("https://www.reformagkh.ru" + building).ConfigureAwait(false);
          return;
        }
      }

      if (Interlocked.Decrement(ref _numberOfLinksLeft) == 0)
      {
        _subject.OnCompleted();
      }
    }

    private async Task AddUrlToQueueAsync(string url) {
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
      Debug.WriteLine(url);
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
