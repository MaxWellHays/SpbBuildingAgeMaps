using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SpbBuildingAgeMaps
{
  static class ExtraLinq
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

    public static async Task<IEnumerable<IQueryable<T>>> BatchAsync<T>(this IQueryable<T> queryable, int batchSize)
    {
      var totalItemCount = await queryable.CountAsync().ConfigureAwait(false);
      return Batch(queryable, batchSize, totalItemCount);
    }

    public static IEnumerable<IQueryable<T>> Batch<T>(this IQueryable<T> queryable, int batchSize, int totalItemCount)
    {
      int counter = 0;
      while (counter < totalItemCount)
      {
        yield return queryable.Skip(counter).Take(batchSize);
        counter += batchSize;
      }
    }
  }
}
