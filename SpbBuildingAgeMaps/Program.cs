using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GeoAPI.Geometries;
using LumenWorks.Framework.IO.Csv;
using MoreLinq;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SpbBuildingAgeMaps.Properties;

namespace SpbBuildingAgeMaps
{
  class Program
  {
    static void Main()
    {
      Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");

      CreateAndFillBuldingTableIfNecessary().Wait();

      return;

      SaveReverseGeocodingResult();
      string geoJsonText;
      using (WebClient client = new WebClient())
      {
        geoJsonText = client.DownloadString("http://polygons.openstreetmap.fr/get_geojson.py?id=1204537");
      }
      JToken t = JObject.Parse(geoJsonText).SelectToken("geometries[0]");
      string poligonJson = t.ToString();
      MultiPolygon geometryCollection = JsonConvert.DeserializeObject<MultiPolygon>(poligonJson,
          new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

      var buildings = GetBuildingsWithCoordsFromResources(Resources.buildingsWithCoords).ToList();

      IGeometryFactory geomFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
      FeatureCollection features = new FeatureCollection();

      int counter = 0;
      int allCount = buildings.Count;
      foreach (Building building in buildings)
      {
        ConsoleHelper.WriteProgress(counter++, allCount);
        GeoHelper.OsmObject osmObject = building.OsmObject;
        IGeometry poligone = osmObject.GetPoligone(geomFactory);
      }
    }

    private static async Task CreateAndFillBuldingTableIfNecessary()
    {
      using (var connection = await SQLiteHelper.GetAndOpenConnetionAsync().ConfigureAwait(false))
      {
        var tableExist = await SQLiteHelper.IsTableExistAsync(connection, "Building").ConfigureAwait(false);

        if (!tableExist)
        {
          throw new NotImplementedException("Table \"Building\" does not exist");
        }

        var buildingCount = await SQLiteHelper.GetBuildingCountAsync(connection).ConfigureAwait(false);
        if (buildingCount == 0)
        {
          Console.WriteLine("Started to fill table");
          var insertedCount = await FillBuildingTableAsync(connection).ConfigureAwait(false);
          Console.WriteLine($"Finish to fill table. Inserted {insertedCount} values");
        }
      }
    }

    private static async Task<int> FillBuildingTableAsync(SQLiteConnection connection)
    {
      var timeDeterrenter = new TimeDeterrenter(ConsoleHelper.WriteProgress, TimeSpan.FromMilliseconds(200));
      var tasks = GetBuildingsFromResources(Resources.SpbBuildingsAge)
        .Batch(500)
        .Select(buildings => SQLiteHelper.InsertBuildingsIntoTableAsync(connection, buildings))
        .Select(task => task.ContinueWith(t => timeDeterrenter.PerformAttempt(t.Result)));
      await Task.WhenAll(tasks).ConfigureAwait(false);
      return timeDeterrenter.TotalAttemptsCount;
    }

    private static void SaveReverseGeocodingResult()
    {
      List<Building> buildings = GetBuildingsFromResources(Resources.SpbBuildingsAge).ToList();

      string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "buildingsWithCoords.csv");
      using (StreamWriter writer = new StreamWriter(path))
      {
        writer.WriteLine("address,buildYear,lon,lat");
        int counter = 0;
        int allCount = buildings.Count;
        foreach (Building building in buildings)
        {
          ConsoleHelper.WriteProgress(counter++, allCount);
          Coordinate coord = building.Coords;
          if (coord == null)
          {
            ConsoleHelper.ColorWriteLine(ConsoleColor.Yellow, "Skipped {0}", counter);
            continue;
          }
          writer.WriteLine("\"{0}\",{1},{2},{3}", building.RawAddress, building.BuildYear,
            coord.X.ToString(CultureInfo.InvariantCulture),
            coord.Y.ToString(CultureInfo.InvariantCulture));
        }
      }
    }

    private static IEnumerable<Building> GetBuildingsFromResources(string buildingsList)
    {
      using (StringReader reader = new StringReader(buildingsList))
      {
        do
        {
          var line = reader.ReadLine();
          if (line == null)
          {
            yield break;
          }
          Building building = Building.Parse(line);
          if (building != null)
          {
            yield return building;
          }
        } while (true);
      }
    }

    private static IEnumerable<Building> GetBuildingsWithCoordsFromResources(string buildingsList)
    {
      using (StringReader stringReader = new StringReader(buildingsList))
      using (var reader = new CsvReader(stringReader, true))
      {
        if (reader.FieldCount != 4)
        {
          throw new NotImplementedException();
        }
        while (reader.ReadNextRecord())
        {
          yield return new Building(reader[0], int.Parse(reader[1]),
            GeoHelper.ParseCoord(reader[2], reader[3]));
        }
      }
    }

    static string[] seps = { "\",", ",\"" };
    static char[] quotes = { '\"', ' ' };
    private static IEnumerable<string> SplitCsvValues(string csvLine)
    {
      return csvLine.Split(seps, StringSplitOptions.None)
        .Select(s => s.Trim(quotes).Replace("\\\"", "\""));
    }

    private static FeatureCollection GetFeatures(IEnumerable<Building> buildings)
    {
      IGeometryFactory geomFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
      FeatureCollection features = new FeatureCollection();
      int counter = 0;
      int allCount = buildings.Count();
      foreach (Building building in buildings)
      {
        ConsoleHelper.WriteProgress(counter++, allCount);
        AttributesTable table = new AttributesTable();
        table.AddAttribute("address", building.RawAddress);
        table.AddAttribute("buildYear", building.BuildYear);
        IGeometry buildingPoligon = building.GetPoligone(geomFactory);
        features.Add(new Feature(buildingPoligon, table));
      }
      return features;
    }
  }
}
