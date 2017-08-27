using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SpbBuildingAgeMaps
{
  static class AddressHelper
  {
    private static readonly string cityName = "Санкт-Петербург";

    private static readonly Regex literCheckerRegex = new Regex(@" ?литер [\p{IsCyrillic}\d]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static IEnumerable<string> NormalizeAddress(string address)
    {
      string normalizeAddress = address.Replace("ул.", "улица").Replace("пер.", "переулок").Replace("корп.", "корпус");
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