using System;
using System.IO;

namespace SpbBuildingAgeMaps
{
  static class SQLiteHelper
  {
    public static string DataDbFilePath
    {
      get
      {
        var projectFolder = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory).Nest(Path.GetDirectoryName, 3);
        return Path.Combine(projectFolder, "data.db");
      }
    }
  }
}
