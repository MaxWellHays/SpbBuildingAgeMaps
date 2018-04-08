using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MoreLinq;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using SpbBuildingAgeMaps.DataModel;

namespace SpbBuildingAgeMaps
{
  class Program
  {
    static async Task Main()
    {
      Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");

      await FillDataAsync().ConfigureAwait(false);

      await ExportDataAsync();
    }

    public static async Task FillDataAsync()
    {
      await FillBuldingTableAsync().ConfigureAwait(false);

      //await FillCoordDataAsync().ConfigureAwait(false);

      await FillReverseGeocodeObjectTableAsync().ConfigureAwait(false);

      await FillPoligonDataAsync().ConfigureAwait(false);
    }

    private static async Task FillPoligonDataAsync()
    {
      using (var db = new BuildingContext())
      {
        var osmObjectsIdWithoutGeometryData = await db.ReverseGeocodeObjects.Where(o => o.OsmObject == null)
            .Select(o => Tuple.Create(o.OsmObjectId, o.Type)).Distinct().ToListAsync().ConfigureAwait(false);
        foreach (var coordDatas in osmObjectsIdWithoutGeometryData.Batch(10))
        {
          var tasks = coordDatas.Select(tuple => GetOsmPoligonDataAsync(tuple.Item1, tuple.Item2)).ToList();
          var osmObjects = await Task.WhenAll(tasks).ConfigureAwait(false);
          await db.OsmObjects.AddRangeAsync(osmObjects).ConfigureAwait(false);
          await db.SaveChangesAsync().ConfigureAwait(false);
        }
      }
    }

    private static readonly GeometryFactory GeometryFactory = new GeometryFactory();

    private static async Task<OsmObject> GetOsmPoligonDataAsync(int osmObjectId, OsmObjectType osmObjectType)
    {
      var geometry = await OsmObjectHelper.GetPoligoneAsync(osmObjectId, osmObjectType, GeometryFactory)
          .ConfigureAwait(false);
      if (geometry == null)
      {
        return null;
      }

      var osmObject = new OsmObject
      {
        OsmObjectId = osmObjectId,
        Source = "",
        GeometryData = geometry.AsBinary(),
      };
      return osmObject;
    }

    private static async Task FillReverseGeocodeObjectTableAsync()
    {
      using (var db = new BuildingContext())
      {
        var coordsWithoutOsmObjects = await db.CoordsData.Where(data => data.ReverseGeocodeObjects.Count == 0)
            .ToListAsync().ConfigureAwait(false);

        HashSet<int> existingOsmObjectId = new HashSet<int>(db.OsmObjects.Select(o => o.OsmObjectId));

        foreach (IEnumerable<CoordData> coordDatas in coordsWithoutOsmObjects.Batch(10))
        {
          List<CoordData> currentCoordsBatch = coordDatas.ToList();
          await Task.WhenAll(currentCoordsBatch.Select(AddReverseGeocodeObjectAsync)).ConfigureAwait(false);

          IEnumerable<ReverseGeocodeObject> reverseObjectsWithoutPoligon = currentCoordsBatch
              .SelectMany(data => data.ReverseGeocodeObjects)
              .Where(reverseObj => !existingOsmObjectId.Contains(reverseObj.OsmObjectId));

          foreach (var reverseGeocodeObject in reverseObjectsWithoutPoligon.DistinctBy(o => o.OsmObjectId))
          {
            var osmObject = await GetOsmPoligonDataAsync(reverseGeocodeObject.OsmObjectId, reverseGeocodeObject.Type).ConfigureAwait(false);
            if (osmObject != null)
            {
              reverseGeocodeObject.OsmObject = osmObject;
              await db.OsmObjects.AddAsync(osmObject).ConfigureAwait(false);
              Debug.Assert(existingOsmObjectId.Add(osmObject.OsmObjectId), "Added existing element to set");
            }
            else
            {
              Debug.Assert(false);
            }
          }

          await db.SaveChangesAsync().ConfigureAwait(false);
        }
      }
    }

    public static async Task AddReverseGeocodeObjectAsync(CoordData coordData)
    {
      var osmObject = await OsmObjectHelper.GetReverseGeocodeObjectByCoordAsync(coordData).ConfigureAwait(false);
      if (osmObject == null)
      {
        return;
      }

      if (coordData.ReverseGeocodeObjects == null)
      {
        coordData.ReverseGeocodeObjects = new List<ReverseGeocodeObject>();
      }

      coordData.ReverseGeocodeObjects.Add(osmObject);
    }

    private static async Task FillCoordDataAsync()
    {
      using (var db = new BuildingContext())
      {
        var buildingsWithoutCoords = await db.Buildings.Where(building => building.CoordsData.Count == 0)
            .ToListAsync().ConfigureAwait(false);
        foreach (var buildings in buildingsWithoutCoords.Batch(100))
        {
          var buildingWithCoords = buildings.Select(AddCoordFromYandexAsync);
          await Task.WhenAll(buildingWithCoords).ConfigureAwait(false);
          await db.SaveChangesAsync().ConfigureAwait(false);
        }
      }
    }

    private static async Task AddCoordFromYandexAsync(Building building)
    {
      var coordData = await GetCoordDataForBuildingFromYandexAsync(building).ConfigureAwait(false);

      if (coordData == null)
      {
        return;
      }

      if (building.CoordsData == null)
      {
        building.CoordsData = new List<CoordData>();
      }

      building.CoordsData.Add(coordData);
    }

    private static async Task<CoordData> GetCoordDataForBuildingFromYandexAsync(Building building)
    {
      var coordOfAddressFromYandex =
          await GeoHelper.GetYandexCoordOfAddressAsync(building.RawAddress).ConfigureAwait(false);
      if (coordOfAddressFromYandex != null)
      {
        var coordData = new CoordData
        {
          BuildingId = building.BuildingId,
          Building = building,
          Coordinate = coordOfAddressFromYandex,
          Source = "Yandex"
        };
        return coordData;
      }

      return null;
    }

    private static async Task FillBuldingTableAsync()
    {
      using (var db = new BuildingContext())
      {
        var buildingCount = await db.Buildings.CountAsync().ConfigureAwait(false);
        if (buildingCount == 0)
        {
          Console.WriteLine("Started to fill table");
          await db.Buildings.AddRangeAsync(RepairDepartmentDataSource.GetBuildings()).ConfigureAwait(false);
          var insertedCount = await db.SaveChangesAsync().ConfigureAwait(false);
          Console.WriteLine($"Finish to fill table. Inserted {insertedCount} values");
        }
      }
    }

    //private static void SaveReverseGeocodingResult()
    //{
    //  List<Building> buildings = RepairDepartmentDataSource.GetBuildings().ToList();

    //  string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "buildingsWithCoords.csv");
    //  using (StreamWriter writer = new StreamWriter(path))
    //  {
    //    writer.WriteLine("address,buildYear,lon,lat");
    //    int counter = 0;
    //    int allCount = buildings.Count;
    //    foreach (Building building in buildings)
    //    {
    //      ConsoleHelper.WriteProgress(counter++, allCount);
    //      Coordinate coord = building.GetCoords();
    //      if (coord == null)
    //      {
    //        ConsoleHelper.ColorWriteLine(ConsoleColor.Yellow, "Skipped {0}", counter);
    //        continue;
    //      }
    //      writer.WriteLine("\"{0}\",{1},{2},{3}", building.RawAddress, building.BuildYear,
    //        coord.X.ToString(CultureInfo.InvariantCulture),
    //        coord.Y.ToString(CultureInfo.InvariantCulture));
    //    }
    //  }
    //}

    static readonly string[] seps = { "\",", ",\"" };
    static readonly char[] quotes = { '\"', ' ' };

    private static IEnumerable<string> SplitCsvValues(string csvLine)
    {
      return csvLine.Split(seps, StringSplitOptions.None)
          .Select(s => s.Trim(quotes).Replace("\\\"", "\""));
    }

    //private static FeatureCollection GetFeatures(IEnumerable<Building> buildings)
    //{
    //  IGeometryFactory geomFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
    //  FeatureCollection features = new FeatureCollection();
    //  int counter = 0;
    //  int allCount = buildings.Count();
    //  foreach (Building building in buildings)
    //  {
    //    ConsoleHelper.WriteProgress(counter++, allCount);
    //    AttributesTable table = new AttributesTable();
    //    table.AddAttribute("address", building.RawAddress);
    //    table.AddAttribute("buildYear", building.BuildYear);
    //    IGeometry buildingPoligon = building.GetPoligone(geomFactory);
    //    features.Add(new Feature(buildingPoligon, table));
    //  }
    //  return features;
    //}

    private static async Task ExportDataAsync()
    {
      using (var dbContext = new BuildingContext())
      {
        var query = dbContext.Buildings
          .Include(building => building.CoordsData)
          .Include(building => building.CoordsData.Select(data => data.ReverseGeocodeObjects))
          .Include(building => building.CoordsData.Select(data => data.ReverseGeocodeObjects.Select(o => o.OsmObject)));

        var buildingForExport = await query.Select(building => new
        {
          building.BuildingId,
          building.RawAddress,
          building.BuildYear,
          building.CoordsData.First().ReverseGeocodeObjects.First().OsmObject.GeometryData
        }).ToListAsync().ConfigureAwait(false);


        WKBReader wkbReader = new WKBReader();
        using (StreamWriter writer = File.CreateText(@"E:\eport"))
        {
          //foreach (var data in buildingForExport)
          //{
          //  var geometry = wkbReader.Read(data.GeometryData);
          //  GeoJsonReader reader = new NetTopologySuite.IO.GeoJsonReader()
          //  var line = string.Join(",", data.BuildingId, data.BuildYear, EscapeAndQuoteString(data.RawAddress), EscapeAndQuoteString(geometry))
          //  await writer.WriteLineAsync();
          //}
        }
      }
    }

    private static string EscapeAndQuoteString(string source)
    {
      return "\"" + source + "\"";
    }
  }
}