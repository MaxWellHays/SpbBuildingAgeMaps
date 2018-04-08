using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpbBuildingAgeMaps
{
  static class AddressHelper
  {
    private static readonly string cityName = "Санкт-Петербург";

    private static readonly Regex literCheckerRegex = new Regex(@" ?литер [\p{IsCyrillic}\d]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Dictionary<string, string> shortWords = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
    {
      {"ул.", "улица"},
      {"пер.", "переулок"},
      {"корп.", "корпус"},
      {"пр.", "проспект"},
      {"пос.", "поселок"},
      {"а.", "аллея"},
      {"о.", "остров"},
      {"г.", "город"},
      {"наб.", "набережная"},
      {"р.", "река"},
      {"пл.", "площадь"},
      {"Ж. Дюкло", "Жака Дюкло"},
    };

    private static readonly Regex shortWordsRegex = new Regex($"(^|\\s)({string.Join("|", shortWords.Keys.Select(s => s.Replace(".","\\.")))})\\s?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static IEnumerable<string> NormalizeAddress(string address)
    {
      var normalizeAddress = address;
      Match match;
      while ((match = shortWordsRegex.Match(normalizeAddress)).Success)
      {
        var key = match.Groups[2].Value;
        var replacement = shortWords[key];
        int position = match.Index + match.Groups[1].Length;
        int length = match.Length - match.Groups[1].Length;
        normalizeAddress = normalizeAddress.Substring(0, position) + replacement + " " + normalizeAddress.Substring(position + length);
      }
      
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