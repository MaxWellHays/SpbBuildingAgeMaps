using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using GeoAPI.Geometries;

namespace SpbBuildingAgeMaps
{
  static class GeoHelper
  {
    public static Coordinate ParseCoord(string coord)
    {
      var values = coord.Split(' ');
      double lon, lat;
      if (values.Length != 2
        || !double.TryParse(values[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out lon)
        || !double.TryParse(values[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out lat))
      {
        return null;
      }
      return new Coordinate(lon, lat);
    }

    public static IEnumerable<string> GetYandexRequests(string rawAddress)
    {
      foreach (string address in AddressHelper.NormalizeAddress(rawAddress))
      {
        yield return string.Format("https://geocode-maps.yandex.ru/1.x/?geocode={0}", HttpUtility.UrlEncode(address));
      }
    }
    public static Coordinate GetYandexCoordOfAddress(string rawAddress)
    {
      foreach (string request in GetYandexRequests(rawAddress))
      {
        XDocument doc = XmlHelper.LoadClearXDocument(request);
        foreach (XElement geoObject in doc.Descendants("GeoObject"))
        {
          string type = geoObject.Descendants("kind").First().Value;
          if (type == "house")
          {
            return ParseCoord(geoObject.Descendants("pos").First().Value);
          }
        }
      }
      return null;
    }

    public class OsmObject
    {
      public int id;
      public OsmObjectType type;

      public static OsmObject GetByCoord(Coordinate coord)
      {
        string nominatimRequest = GetNominatimRequest(coord);
        XDocument doc = XDocument.Parse(WebHelper.DownloadString(nominatimRequest));
        IEnumerable<XElement> resultXElement = doc.Descendants("result");
        OsmObjectType osmType = (OsmObjectType)Enum.Parse(typeof(OsmObjectType), resultXElement.Attributes("osm_type").First().Value, true);
        int osmId = int.Parse(resultXElement.Attributes("osm_id").First().Value);
        return new OsmObject() { id = osmId, type = osmType };
      }

      public IGeometry GetPoligone(IGeometryFactory geometryFactory)
      {
        switch (type)
        {
          case GeoHelper.OsmObjectType.Way:
            return GetPoligonOfWay(id, geometryFactory);
          case GeoHelper.OsmObjectType.Relation:
            return GetMultypoligonOfRelation(id, geometryFactory);
          default:
            throw new NotImplementedException();
        }
      }
      public static IPolygon GetPoligonOfWay(int wayId, IGeometryFactory geometryFactory)
      {
        string wayLink = string.Format("https://www.openstreetmap.org/api/0.6/way/{0}", wayId);
        XDocument wayObject = XmlHelper.LoadXDocument(wayLink);
        return geometryFactory.CreatePolygon(
          wayObject.Descendants("nd")
            .Attributes("ref")
            .Select(attribute => GetOsmNodeCoord(attribute.Value))
            .ToArray());
      }

      public static IGeometry GetMultypoligonOfRelation(int relationId, IGeometryFactory geometryFactory)
      {
        string relationLink = string.Format("https://www.openstreetmap.org/api/0.6/relation/{0}", relationId);
        XDocument relationObject = XmlHelper.LoadXDocument(relationLink);
        IEnumerable<int> wayIndexes = relationObject.Descendants("member").Where(element => element.Attribute("type").Value.Equals("way", StringComparison.CurrentCultureIgnoreCase))
          .Attributes("ref").Select(attribute => int.Parse(attribute.Value)).ToArray();
        return geometryFactory.CreateMultiPolygon(wayIndexes.Select(wayId => GetPoligonOfWay(wayId, geometryFactory)).ToArray());
      }
    }

    public enum OsmObjectType
    {
      Way,
      Relation
    }

    public static Coordinate GetOsmNodeCoord(string nodeId)
    {
      string nodeLink = string.Format("https://www.openstreetmap.org/api/0.6/node/{0}", nodeId);
      var nodeDoc = XmlHelper.LoadXDocument(nodeLink);
      string latValue = nodeDoc.Descendants("node").Attributes("lat").First().Value;
      double lat = double.Parse(latValue.Trim('"').Replace(".", ","));
      string lonValue = nodeDoc.Descendants("node").Attributes("lon").First().Value;
      double lon = double.Parse(lonValue.Trim('"').Replace(".", ","));
      return new Coordinate(lon, lat);
    }

    public static string GetNominatimRequest(Coordinate coord)
    {
      string nominatimRequest =
        string.Format("http://nominatim.openstreetmap.org/reverse.php?format=xml&lat={0}&lon={1}&zoom=18", coord.Y,
          coord.X);
      return nominatimRequest;
    }
  }
}
