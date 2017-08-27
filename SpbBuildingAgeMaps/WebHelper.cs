using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SpbBuildingAgeMaps
{
  static class WebHelper
  {
    private static readonly WebClient client = new WebClient
    {
      Encoding = Encoding.UTF8,
      Headers = { [HttpRequestHeader.UserAgent] = userAgent }
    };

    const string userAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";

    public static string DownloadString(string url)
    {
      return client.DownloadString(url);
    }

    public static async Task<string> DownloadStringAsync(string url)
    {
      var request = (HttpWebRequest)WebRequest.Create(url);
      request.UserAgent = userAgent;
      request.Method = "GET";
      var webResponse = await request.GetResponseAsync().ConfigureAwait(false);
      using (var responseStream = webResponse.GetResponseStream())
      using (var memoryStream = await responseStream.CopyToMemoryStreamAsync().ConfigureAwait(false))
      {
        return Encoding.UTF8.GetString(memoryStream.ToArray());
      }
    }

    public static async Task<MemoryStream> CopyToMemoryStreamAsync(this Stream sourceStream)
    {
      var memoryStream = new MemoryStream();
      await sourceStream.CopyToAsync(memoryStream).ConfigureAwait(false);
      memoryStream.Position = 0;
      return memoryStream;
    }
  }
}