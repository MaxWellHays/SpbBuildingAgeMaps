using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using GeoAPI.Geometries;
using NetTopologySuite;
using NetTopologySuite.Features;

namespace SpbBuildingAgeMaps
{
  class Program
  {
    static void Main()
    {
      List<Building> buildings = GetBuildingsFromFile(Assembly.GetExecutingAssembly().GetManifestResourceNames().First());

      //IGeometryFactory geomFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
      //var t1 = buildings.First(building => building.Index == 14).GetPoligone(geomFactory);
      //var t2 = buildings.First(building => building.Index == 15).GetPoligone(geomFactory);

      var features = GetFeatures(buildings.Take(200));
      string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test3.json");
      var jsonSerializer = new NetTopologySuite.IO.GeoJsonSerializer();
      using (StreamWriter writer = new StreamWriter(path))
      {
        jsonSerializer.Serialize(writer, features);
      }
    }

    private static List<Building> GetBuildingsFromFile(string filePath)
    {
      var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filePath);
      var streamReader = new StreamReader(stream);
      streamReader.ReadLine();
      List<Building> buildings = new List<Building>();
      do
      {
        Building building = Building.Parse(streamReader.ReadLine());
        if (building != null)
        {
          buildings.Add(building);
        }
      } while (!streamReader.EndOfStream);
      return buildings;
    }

    private static FeatureCollection GetFeatures(IEnumerable<Building> buildings)
    {
      IGeometryFactory geomFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
      FeatureCollection features = new FeatureCollection();
      int counter = 0;
      int allCount = buildings.Count();
      foreach (Building building in buildings)
      {
        Console.Write("Progress {0}/{1}", ++counter, allCount);
        Console.CursorLeft = 0;
        AttributesTable table = new AttributesTable();
        table.AddAttribute("address", building.RawAddress);
        table.AddAttribute("buildYear", building.BuildYear);
        IGeometry buildingPoligon = building.GetPoligone(geomFactory);
        features.Add(new Feature(buildingPoligon, table));
      }
      return features;
    }
  }

  class Building
  {
    public readonly int Index;
    public readonly string RawAddress;
    private string type;
    private string district;
    public readonly int BuildYear;

    private int osmId;
    private OsmObjectType osmType;

    public Building(int index, string rawAddress, string type, string district, int buildYear)
    {
      this.Index = index;
      this.RawAddress = rawAddress;
      this.type = type;
      this.district = district;
      this.BuildYear = buildYear;
    }

    public static Building Parse(string paramsLine)
    {
      var buildingParams = paramsLine.Split('\t');
      int index = int.Parse(buildingParams[0]);
      string rawAddress = buildingParams[1];
      string type = buildingParams[2];
      string district = buildingParams[3];
      int buildYear;
      if (int.TryParse(buildingParams[4], out buildYear))
      {
        return new Building(index, rawAddress, type, district, buildYear);
      }
      return null;
    }

    public enum OsmObjectType
    {
      Way,
      Relation
    }

    public Tuple<OsmObjectType, int> GetOsmObject()
    {
      if (osmId == 0)
      {
        string coord = FindCoords();
        if (coord != null)
        {
          var nominatimRequest = GetNominatimRequest(coord);
          var doc = XDocument.Parse(WebHelper.DownloadString(nominatimRequest));
          IEnumerable<XElement> resultXElement = doc.Descendants("result");
          osmType = (OsmObjectType)Enum.Parse(typeof(OsmObjectType), resultXElement.Attributes("osm_type").First().Value, true);
          osmId = int.Parse(resultXElement.Attributes("osm_id").First().Value);
        }
      }
      return new Tuple<OsmObjectType, int>(osmType, osmId);
    }

    private static string GetNominatimRequest(string coord)
    {
      var coordParts = coord.Split(' ');
      string nominatimRequest =
        string.Format("http://nominatim.openstreetmap.org/reverse.php?format=xml&lat={0}&lon={1}&zoom=18", coordParts[1],
          coordParts[0]);
      return nominatimRequest;
    }

    private string FindCoords()
    {
      foreach (string request in YandexRequests)
      {
        XDocument doc = XmlHelper.LoadClearXDocument(request);
        foreach (XElement geoObject in doc.Descendants("GeoObject"))
        {
          string type = geoObject.Descendants("kind").First().Value;
          if (type == "house")
          {
            return geoObject.Descendants("pos").First().Value;
          }
        }
      }
      return null;
    }

    public IEnumerable<string> YandexRequests
    {
      get
      {
        foreach (string address in AddressHelper.NormalizeAddress(RawAddress))
        {
          yield return string.Format("https://geocode-maps.yandex.ru/1.x/?geocode={0}", HttpUtility.UrlEncode(address));
        }
      }
    }

    private static Coordinate GetNodeCoord(string nodeId)
    {
      string nodeLink = string.Format("https://www.openstreetmap.org/api/0.6/node/{0}", nodeId);
      var nodeDoc = XmlHelper.LoadXDocument(nodeLink);
      string latValue = nodeDoc.Descendants("node").Attributes("lat").First().Value;
      double lat = double.Parse(latValue.Trim('"').Replace(".", ","));
      string lonValue = nodeDoc.Descendants("node").Attributes("lon").First().Value;
      double lon = double.Parse(lonValue.Trim('"').Replace(".", ","));
      return new Coordinate(lon, lat);
    }

    public IGeometry GetPoligone(IGeometryFactory geometryFactory)
    {
      Tuple<OsmObjectType, int> osmObject = GetOsmObject();
      switch (osmObject.Item1)
      {
        case OsmObjectType.Way:
          return GetPoligonOfWay(osmObject.Item2, geometryFactory);
        case OsmObjectType.Relation:
          return GetMultypoligonOfRelation(osmObject.Item2, geometryFactory);
        default:
          throw new NotImplementedException();
      }
    }

    public IPolygon GetPoligonOfWay(int wayId, IGeometryFactory geometryFactory)
    {
      string wayLink = string.Format("https://www.openstreetmap.org/api/0.6/way/{0}", wayId);
      XDocument wayObject = XmlHelper.LoadXDocument(wayLink);
      return geometryFactory.CreatePolygon(
        wayObject.Descendants("nd")
          .Attributes("ref")
          .Select(attribute => GetNodeCoord(attribute.Value))
          .ToArray());
    }

    public IGeometry GetMultypoligonOfRelation(int relationId, IGeometryFactory geometryFactory)
    {
      string relationLink = string.Format("https://www.openstreetmap.org/api/0.6/relation/{0}", relationId);
      XDocument relationObject = XmlHelper.LoadXDocument(relationLink);
      IEnumerable<int> wayIndexes = relationObject.Descendants("member").Where(element => element.Attribute("type").Value.Equals("way", StringComparison.CurrentCultureIgnoreCase))
        .Attributes("ref").Select(attribute => int.Parse(attribute.Value)).ToArray();
      return geometryFactory.CreateMultiPolygon(wayIndexes.Select(wayId => GetPoligonOfWay(wayId, geometryFactory)).ToArray());
    }
  }

  static class AddressHelper
  {
    private static string cityName = "Санкт-Петербург";

    private static Regex literCheckerRegex = new Regex(" ?литер [\\w]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static IEnumerable<string> NormalizeAddress(string address)
    {
      string normalizeAddress = address.Replace("ул.", "улица").Replace("пер.", "переулок").Replace("пер.", "переулок");
      normalizeAddress = $"{cityName} {normalizeAddress}";
      yield return normalizeAddress;

      var literMatch = literCheckerRegex.Match(normalizeAddress);
      if (literMatch.Success)
      {
        yield return normalizeAddress.Substring(0, literMatch.Index);
      }
    }
  }

  static class XmlHelper
  {
    static readonly Regex xmlnsRepaceRegex = new Regex("( (?!version|encoding)(\\w+))(:\\w+)?=\"[^\"]*\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static XDocument LoadXDocument(string request)
    {
      return XDocument.Parse(WebHelper.DownloadString(request));
    }

    public static XDocument LoadClearXDocument(string request)
    {
      return XDocument.Parse(xmlnsRepaceRegex.Replace(WebHelper.DownloadString(request), string.Empty));
    }
  }

  static class WebHelper
  {
    private static WebClient client = new WebClient() { Encoding = Encoding.UTF8 };
    const string userAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";


    public static string DownloadString(string url)
    {
      client.Headers.Add("user-agent", userAgent);
      return client.DownloadString(url);
    }
  }

  static class ConsoleHelper
  {
    public static void ColorWriteLine(ConsoleColor color, string format, params object[] formatParams)
    {
      ConsoleColor keepColor = Console.ForegroundColor;
      Console.ForegroundColor = color;
      Console.WriteLine(format, formatParams);
      Console.ForegroundColor = keepColor;
    }

    public static void ErrorWriteLine(string format, params object[] formatParams)
    {
      ColorWriteLine(ConsoleColor.Red, format, formatParams);
    }
  }
}
