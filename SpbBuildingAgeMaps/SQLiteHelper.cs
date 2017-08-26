using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SpbBuildingAgeMaps.Properties;

namespace SpbBuildingAgeMaps
{
  static class SQLiteHelper
  {
    public static async Task<SQLiteConnection> GetAndOpenConnetionAsync()
    {
      var projectFolder = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory).Nest(Path.GetDirectoryName, 2);
      var dataDbFilePath = Path.Combine(projectFolder, "data.db");
      var connection = new SQLiteConnection($"Data Source={dataDbFilePath};Version=3;");
      connection.Trace += (sender, args) => { Debug.WriteLine($"SQLiteConnection handle query: {args.Statement}"); };
      await connection.OpenAsync().ConfigureAwait(false);
      return connection;
    }

    public static async Task<bool> IsTableExistAsync(SQLiteConnection connection, string tableName)
    {
      SQLiteCommand command = new SQLiteCommand($"SELECT name FROM sqlite_master WHERE type = 'table' AND name = '{tableName}';", connection);
      var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

      while (await reader.ReadAsync().ConfigureAwait(false))
      {
        if (!string.Equals(reader[0].ToString(), tableName, StringComparison.OrdinalIgnoreCase))
        {
          return false;
        }
      }

      return true;
    }

    public static async Task InsertBuildingIntoTableAsync(SQLiteConnection connection, Building building)
    {
      SQLiteCommand insertSQL = new SQLiteCommand("INSERT INTO Building (Id, Address, Type, District, BuildYear) VALUES (@Id, @Address, @Type, @District, @BuildYear)", connection);
      insertSQL.Parameters.AddWithValue("@Id", building.Id);
      insertSQL.Parameters.AddWithValue("@Address", building.RawAddress);
      insertSQL.Parameters.AddWithValue("@Type", building.BuildingType);
      insertSQL.Parameters.AddWithValue("@District", building.District);
      insertSQL.Parameters.AddWithValue("@BuildYear", building.BuildYear);
      var queryText = insertSQL.CommandText;
      var result = await insertSQL.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public static async Task<int> GetBuildingCountAsync(SQLiteConnection connection)
    {
      SQLiteCommand cmd = new SQLiteCommand("SELECT COUNT(Id) FROM Building", connection);
      var result = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
      return !Convert.IsDBNull(result) ? (int)(long)result : 0;
    }
  }
}
