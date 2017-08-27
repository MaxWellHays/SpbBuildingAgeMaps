using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SpbBuildingAgeMaps
{
  static class XmlHelper
  {
    static readonly Regex xmlnsRepaceRegex = new Regex("( (?!version|encoding)(\\w+))(:\\w+)?=\"[^\"]*\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static XDocument LoadXDocument(string request)
    {
      return XDocument.Parse(WebHelper.DownloadString(request));
    }

    public static async Task<XDocument> LoadXDocumentAsync(string request)
    {
      var downloadString = await WebHelper.DownloadStringAsync(request).ConfigureAwait(false);
      return XDocument.Parse(downloadString);
    }

    public static XDocument LoadClearXDocument(string request)
    {
      return XDocument.Parse(xmlnsRepaceRegex.Replace(WebHelper.DownloadString(request), string.Empty));
    }

    public static async Task<XDocument> LoadClearXDocumentAsync(string request)
    {
      var downloadString = await WebHelper.DownloadStringAsync(request).ConfigureAwait(false);
      return XDocument.Parse(xmlnsRepaceRegex.Replace(downloadString, string.Empty));
    }
  }
}