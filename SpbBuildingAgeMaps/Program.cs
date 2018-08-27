using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MoreLinq;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using SpbBuildingAgeMaps.DataModel;
using SpbBuildingAgeMaps.DataProviders;

namespace SpbBuildingAgeMaps
{
  class Program
  {
    static async Task Main()
    {
      Thread.CurrentThread.CurrentCulture = new CultureInfo("en-us");
			Directory.CreateDirectory("DataCache");
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      //await new BuildingInfoProvider().GenerateCityDataFileAsync();
      ReformaGhkBuidlingsProvider reformaGhkBuidlingsProvider = new ReformaGhkBuidlingsProvider();
      IDisposable subscription =
        Observable.Using(() => File.Create("buildingsFromReformaGhk.txt"),
            stream => Observable.Using(() => new StreamWriter(stream),
              writer => reformaGhkBuidlingsProvider.Select(entry => new { entry, writer })))
          .Subscribe(async x =>
          {
            string buildingInfo = $"{x.entry.Address} {x.entry.BuildYear} {x.entry.Coordinate}";
            await x.writer.WriteLineAsync(buildingInfo).ConfigureAwait(false);
            Console.WriteLine(buildingInfo);
          });

      await reformaGhkBuidlingsProvider.ToTask().ConfigureAwait(false);
    }

    public static async Task FillDataAsync()
    {
      //await FillBuldingTableAsync().ConfigureAwait(false);

      //await FillCoordDataAsync().ConfigureAwait(false);

      //await FillReverseGeocodeObjectTableAsync().ConfigureAwait(false);

      //await FillPoligonDataAsync().ConfigureAwait(false);
    }

    //private static async Task FillPoligonDataAsync()
    //{
    //  using (var db = new BuildingContext())
    //  {
    //    var osmObjectsIdWithoutGeometryData = await db.ReverseGeocodeObjects.Where(o => o.OsmObject == null)
    //        .Select(o => Tuple.Create(o.OsmObjectId, o.Type)).Distinct().ToListAsync().ConfigureAwait(false);
    //    foreach (var coordDatas in osmObjectsIdWithoutGeometryData.Batch(10))
    //    {
    //      var tasks = coordDatas.Select(tuple => GetOsmPoligonDataAsync(tuple.Item1, tuple.Item2)).ToList();
    //      var osmObjects = await Task.WhenAll(tasks).ConfigureAwait(false);
    //      await db.OsmObjects.AddRangeAsync(osmObjects).ConfigureAwait(false);
    //      await db.SaveChangesAsync().ConfigureAwait(false);
    //    }
    //  }
    //}

    //private static readonly ThreadLocal<GeometryFactory> GeometryFactory = new ThreadLocal<GeometryFactory>(()=> new GeometryFactory());
    //private static readonly ThreadLocal<WKBWriter> BinaryWriter = new ThreadLocal<WKBWriter>(() => new WKBWriter());
    //private static readonly ThreadLocal<WKBReader> BinaryReader = new ThreadLocal<WKBReader>(() => new WKBReader());

    //private static async Task<OsmObject> GetOsmPoligonDataAsync(int osmObjectId, OsmObjectType osmObjectType)
    //{
    //  var geometry = await OsmObjectHelper.GetPoligoneAsync(osmObjectId, osmObjectType, GeometryFactory.Value).ConfigureAwait(false);
    //  if (geometry == null)
    //  {
    //    return null;
    //  }

    //  var osmObject = new OsmObject
    //  {
    //    OsmObjectId = osmObjectId,
    //    Source = "",
    //    GeometryData = BinaryWriter.Value.Write(geometry),
    //  };

    //  return osmObject;
    //}

    //private static async Task FillReverseGeocodeObjectTableAsync()
    //{
    //  using (var db = new BuildingContext())
    //  {
    //    var coordsWithoutOsmObjects = await db.CoordsData.Where(data => data.ReverseGeocodeObjects.Count == 0)
    //        .ToListAsync().ConfigureAwait(false);

    //    HashSet<int> existingOsmObjectId = new HashSet<int>(db.OsmObjects.Select(o => o.OsmObjectId));

    //    foreach (IEnumerable<CoordData> coordDatas in coordsWithoutOsmObjects.Batch(10))
    //    {
    //      List<CoordData> currentCoordsBatch = coordDatas.ToList();
    //      await Task.WhenAll(currentCoordsBatch.Select(AddReverseGeocodeObjectAsync)).ConfigureAwait(false);

    //      var reverseObjectsWithoutPoligon = currentCoordsBatch
    //          .SelectMany(data => data.ReverseGeocodeObjects ?? Enumerable.Empty<ReverseGeocodeObject>())
    //          .Where(reverseObj => reverseObj != null && !existingOsmObjectId.Contains(reverseObj.OsmObjectId));

    //      foreach (var reverseGeocodeObject in reverseObjectsWithoutPoligon.DistinctBy(o => o.OsmObjectId))
    //      {
    //        var osmObject = await GetOsmPoligonDataAsync(reverseGeocodeObject.OsmObjectId, reverseGeocodeObject.Type).ConfigureAwait(false);
    //        if (osmObject != null)
    //        {
    //          reverseGeocodeObject.OsmObject = osmObject;
    //          await db.OsmObjects.AddAsync(osmObject).ConfigureAwait(false);
    //          Debug.Assert(existingOsmObjectId.Add(osmObject.OsmObjectId), "Added existing element to set");
    //        }
    //        else
    //        {
    //          Debug.Assert(false);
    //        }
    //      }

    //      await db.SaveChangesAsync().ConfigureAwait(false);
    //    }
    //  }
    //}

    //public static async Task AddReverseGeocodeObjectAsync(CoordData coordData)
    //{
    //  var osmObject = await OsmObjectHelper.GetReverseGeocodeObjectByCoordAsync(coordData).ConfigureAwait(false);
    //  if (osmObject == null)
    //  {
    //    return;
    //  }

    //  if (coordData.ReverseGeocodeObjects == null)
    //  {
    //    coordData.ReverseGeocodeObjects = new List<ReverseGeocodeObject>();
    //  }

    //  coordData.ReverseGeocodeObjects.Add(osmObject);
    //}

    //private static async Task FillCoordDataAsync()
    //{
    //  using (var db = new BuildingContext())
    //  {
    //    var buildingsWithoutCoords = await db.Buildings.Where(building => building.CoordsData.Count == 0)
    //        .ToListAsync().ConfigureAwait(false);
    //    foreach (var buildings in buildingsWithoutCoords.Batch(10))
    //    {
    //      var buildingWithCoords = buildings.Select(AddCoordFromYandexAsync);
    //      await Task.WhenAll(buildingWithCoords).ConfigureAwait(false);
    //      await db.SaveChangesAsync().ConfigureAwait(false);
    //    }
    //  }
    //}

    //private static async Task AddCoordFromYandexAsync(BuildingInfo buildingInfo)
    //{
    //  var coordData = await GetCoordDataForBuildingFromYandexAsync(buildingInfo).ConfigureAwait(false);
    //  if (coordData == null)
    //  {
    //    return;
    //  }
    //  if (buildingInfo.CoordsData == null)
    //  {
    //    buildingInfo.CoordsData = new List<CoordData>();
    //  }
    //  buildingInfo.CoordsData.Add(coordData);
    //}

    private static async Task<CoordData> GetCoordDataForBuildingFromYandexAsync(BuildingInfo buildingInfo)
    {
      var coordOfAddressFromYandex = await GeoHelper.GetYandexCoordOfAddressAsync(buildingInfo.Address).ConfigureAwait(false);
      if (coordOfAddressFromYandex != null)
      {
        var coordData = new CoordData
        {
          BuildingInfo = buildingInfo,
          Coordinate = coordOfAddressFromYandex,
          Source = "Yandex"
        };
        return coordData;
      }

      return null;
    }

    //private static async Task FillBuldingTableAsync()
    //{
    //  using (var db = new BuildingContext())
    //  {
    //    var buildingCount = await db.Buildings.CountAsync().ConfigureAwait(false);
    //    if (buildingCount == 0)
    //    {
    //      Console.WriteLine("Started to fill table");
    //      await db.Buildings.AddRangeAsync(RepairDepartmentDataSource.GetBuildings()).ConfigureAwait(false);
    //      var insertedCount = await db.SaveChangesAsync().ConfigureAwait(false);
    //      Console.WriteLine($"Finish to fill table. Inserted {insertedCount} values");
    //    }
    //  }
    //}

//    private static void SaveReverseGeocodingResult()
//    {
//      List<BuildingInfo> buildings = RepairDepartmentDataSource.GetBuildings().ToList();
//
//      string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "buildingsWithCoords.csv");
//      using (StreamWriter writer = new StreamWriter(path))
//      {
//        writer.WriteLine("address,buildYear,lon,lat");
//        int counter = 0;
//        int allCount = buildings.Count;
//        foreach (BuildingInfo buildingInfo in buildings)
//        {
//          ConsoleHelper.WriteProgress(counter++, allCount);
//          Coordinate coord = buildingInfo.GetCoords();
//          if (coord == null)
//          {
//            ConsoleHelper.ColorWriteLine(ConsoleColor.Yellow, "Skipped {0}", counter);
//            continue;
//          }
//          writer.WriteLine("\"{0}\",{1},{2},{3}", buildingInfo.Address, buildingInfo.BuildYear,
//            coord.X.ToString(CultureInfo.InvariantCulture),
//            coord.Y.ToString(CultureInfo.InvariantCulture));
//        }
//      }
//    }

    static readonly string[] seps = { "\",", ",\"" };
    static readonly char[] quotes = { '\"', ' ' };

    private static IEnumerable<string> SplitCsvValues(string csvLine)
    {
      return csvLine.Split(seps, StringSplitOptions.None)
          .Select(s => s.Trim(quotes).Replace("\\\"", "\""));
    }

//    private static FeatureCollection GetFeatures(IEnumerable<BuildingInfo> buildings)
//    {
//      IGeometryFactory geomFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
//      FeatureCollection features = new FeatureCollection();
//      int counter = 0;
//      int allCount = buildings.Count();
//      foreach (BuildingInfo buildingInfo in buildings)
//      {
//        ConsoleHelper.WriteProgress(counter++, allCount);
//        AttributesTable table = new AttributesTable();
//        table.AddAttribute("address", buildingInfo.Address);
//        table.AddAttribute("buildYear", buildingInfo.BuildYear);
//        IGeometry buildingPoligon = buildingInfo.GetPoligone(geomFactory);
//        features.Add(new Feature(buildingPoligon, table));
//      }
//      return features;
//    }

    public class BuildingExportData
    {
      public int BuildingId { get; }
      public string RawAddress { get; }
      public int BuildYear { get; }
      public byte[] GeometryData { get; }

      public BuildingExportData(int buildingId, string rawAddress, int buildYear, byte[] geometryData)
      {
        BuildingId = buildingId;
        RawAddress = rawAddress;
        BuildYear = buildYear;
        GeometryData = geometryData;
      }
    }

//    private static async Task ExportDataAsync()
//    {
//      using (var dbContext = new BuildingContext())
//      {
//        var query = dbContext.Buildings
//          .Include(building => building.CoordsData)
//          .ThenInclude(data => data.ReverseGeocodeObjects)
//          .ThenInclude(reverseObj => reverseObj.OsmObject);
//
//        var buildingForExport = await query.Select(building => new BuildingExportData(building.BuildingInfoId,
//            building.RawAddress, building.BuildYear,
//            building.CoordsData.SelectMany(data => data.ReverseGeocodeObjects.Select(o => o.OsmObject.GeometryData)).FirstOrDefault()))
//          .Where(data => data.GeometryData != null)
//          .ToListAsync().ConfigureAwait(false);
//
//        WKBReader wkbReader = BinaryReader.Value;
//        GeoJsonWriter jsonWriter = new GeoJsonWriter();
//        using (StreamWriter writer = File.CreateText(@"E:\export.csv"))
//        {
//          await writer.WriteLineAsync(string.Join(",", "build_year", "address", "geojson")).ConfigureAwait(false);
//          foreach (var data in buildingForExport)
//          {
//            var geometry = wkbReader.Read(data.GeometryData);
//            var geometryGeoJson = jsonWriter.Write(geometry);
//            var line = string.Join(",", data.BuildYear, EscapeAndQuoteString(data.RawAddress),
//              EscapeAndQuoteString(geometryGeoJson));
//            await writer.WriteLineAsync(line).ConfigureAwait(false);
//          }
//        }
//      }
//    }

    private static string EscapeAndQuoteString(string source)
    {
      return "\"" + source.Replace("\"","\"\"") + "\"";
    }
  }
}