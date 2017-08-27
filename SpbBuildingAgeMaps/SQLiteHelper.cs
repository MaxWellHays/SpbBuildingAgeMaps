using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SQLite;

namespace SpbBuildingAgeMaps
{
  static class SQLiteHelper
  {
    public static string DataDbFilePath
    {
      get
      {
        var projectFolder = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory).Nest(Path.GetDirectoryName, 2);
        return Path.Combine(projectFolder, "data.db");
      }
    }

    public static SQLiteAsyncConnection GetConnetion()
    {
      return new SQLiteAsyncConnection(DataDbFilePath);
    }

    public static async Task<IEnumerable<Task<List<T>>>> BatchAsync<T>(this AsyncTableQuery<T> table, int batchSize)
      where T : new()
    {
      var itemsTotalCount = await table.CountAsync().ConfigureAwait(false);
      return BatchAsync(table, batchSize, itemsTotalCount);
    }

    public static IEnumerable<Task<List<T>>> BatchAsync<T>(this AsyncTableQuery<T> table, int batchSize, int itemsTotalCount)
      where T : new()
    {
      int counter = 0;
      while (counter < itemsTotalCount)
      {
        yield return table.Skip(counter).Take(batchSize).ToListAsync();
        counter += batchSize;
      }
    }
  }
}
