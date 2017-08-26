using System;
using System.Collections.Generic;
using System.Linq;

namespace SpbBuildingAgeMaps
{
  static class ExtraLINQ
  {
    public static T Nest<T>(this T source, Func<T, T> projection, int n)
    {
      return NestList(source, projection, n + 1).Last();
    }

    public static IEnumerable<T> NestList<T>(this T source, Func<T, T> projection, int n)
    {
      var current = source;
      int counter = 0;
      while (counter++ < n)
      {
        yield return current;
        current = projection(current);
      }
    }

    public static IEnumerable<T> NestWhileList<T>(this T source, Func<T, T> projection, Predicate<T> whileCondition)
    {
      var current = source;
      while (whileCondition(current))
      {
        yield return current;
        current = projection(current);
      }
    }
  }
}
