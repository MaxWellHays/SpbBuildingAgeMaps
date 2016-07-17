using System.Net;
using System.Text;

namespace SpbBuildingAgeMaps
{
  static class WebHelper
  {
    private static WebClient client = new WebClient() { Encoding = Encoding.UTF8 };
    const string userAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";

    public static string DownloadString(string url)
    {
      client.Headers.Add("user-agent", userAgent);
      return client.DownloadString(url);
    }
  }
}