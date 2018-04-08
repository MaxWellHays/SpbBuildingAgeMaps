using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Nito.AsyncEx;

namespace SpbBuildingAgeMaps
{
  static class ConsoleHelper
  {
    private static readonly AsyncLock mutex = new AsyncLock();

    [StringFormatMethod("format")]
    public static async Task ColorWriteLineAsync(ConsoleColor color, string format, params object[] formatParams)
    {
      using (await mutex.LockAsync().ConfigureAwait(false))
      {
        ConsoleColor keepColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(format, formatParams);
        Console.ForegroundColor = keepColor;
      }
    }

    public static Task ErrorWriteLineAsync(string format, params object[] formatParams)
    {
      return ColorWriteLineAsync(ConsoleColor.Red, format, formatParams);
    }

    public static async Task WriteProgressAsync(int processed, int fullCount)
    {
      using (await mutex.LockAsync().ConfigureAwait(false))
      {
        Console.Write("Progress {0}/{1}", processed, fullCount);
        Console.CursorLeft = 0;
      }
    }

    public static async Task WriteProgressAsync(int processed)
    {
      using (await mutex.LockAsync().ConfigureAwait(false))
      {
        Console.Write("Progress {0}", processed);
        Console.CursorLeft = 0;
      }
    }
  }
}