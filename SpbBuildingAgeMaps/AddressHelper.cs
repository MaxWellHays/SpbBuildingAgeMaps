using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SpbBuildingAgeMaps
{
  static class AddressHelper
  {
    private static string cityName = "�����-���������";

    private static Regex literCheckerRegex = new Regex(" ?����� [\\w]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static IEnumerable<string> NormalizeAddress(string address)
    {
      string normalizeAddress = address.Replace("��.", "�����").Replace("���.", "��������").Replace("���.", "��������");
      normalizeAddress = $"{cityName} {normalizeAddress}";
      yield return normalizeAddress;

      var literMatch = literCheckerRegex.Match(normalizeAddress);
      if (literMatch.Success)
      {
        yield return normalizeAddress.Substring(0, literMatch.Index);
      }
    }
  }
}