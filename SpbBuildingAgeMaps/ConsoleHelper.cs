using System;

namespace SpbBuildingAgeMaps
{
  static class ConsoleHelper
  {
    public static void ColorWriteLine(ConsoleColor color, string format, params object[] formatParams)
    {
      ConsoleColor keepColor = Console.ForegroundColor;
      Console.ForegroundColor = color;
      Console.WriteLine(format, formatParams);
      Console.ForegroundColor = keepColor;
    }

    public static void ErrorWriteLine(string format, params object[] formatParams)
    {
      ColorWriteLine(ConsoleColor.Red, format, formatParams);
    }

    public static void WriteProgress(int processed, int fullCount)
    {
      Console.Write("Progress {0}/{1}", processed, fullCount);
      Console.CursorLeft = 0;
    }
  }
}