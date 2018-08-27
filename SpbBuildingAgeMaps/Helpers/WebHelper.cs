using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SpbBuildingAgeMaps
{
  static class WebHelper
  {
    public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36";

    public static async Task<string> DownloadStringAsync(string url)
    {
      using (Stream responseStream = await DownloadFileAsync(url).ConfigureAwait(false))
      using (MemoryStream memoryStream = await responseStream.CopyToMemoryStreamAsync().ConfigureAwait(false))
      {
        return Encoding.UTF8.GetString(memoryStream.ToArray());
      }
    }

    public static async Task<Stream> DownloadFileAsync(string url)
    {
      HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
      request.UserAgent = UserAgent;
      request.Method = "GET";
      request.Proxy = null;
      WebResponse webResponse = await request.GetResponseAsync().ConfigureAwait(false);
      return webResponse.GetResponseStream();
    }

    public static async Task<WebResponse> PostRequest(string url, byte[] content)
    {
      HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
      request.UserAgent = UserAgent;
      request.Method = "POST";
      request.Proxy = null;
      Stream requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false);
      await requestStream.WriteAsync(content, 0, content.Length).ConfigureAwait(false);
      requestStream.Close();
      return await request.GetResponseAsync().ConfigureAwait(false);
    }

    public static async Task<MemoryStream> CopyToMemoryStreamAsync(this Stream sourceStream)
    {
      MemoryStream memoryStream = new MemoryStream();
      await sourceStream.CopyToAsync(memoryStream).ConfigureAwait(false);
      memoryStream.Position = 0;
      return memoryStream;
    }
  }
}
