using System;
using System.IO;
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
  }
}
