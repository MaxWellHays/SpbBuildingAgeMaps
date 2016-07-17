using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using GeoAPI.Geometries;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SpbBuildingAgeMaps
{
  static class GeoHelper
  {
    public static Coordinate ParseCoord(string lonAndLat)
    {
      var values = lonAndLat.Split(' ');
      if (values.Length != 2)
      {
        return null;
      }
      return ParseCoord(values[0], values[1]);
    }

    public static Coordinate ParseCoord(string lon, string lat)
    {
      double lonValue, latValue;
      if (!double.TryParse(lon, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out lonValue)
        || !double.TryParse(lat, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out latValue))
      {
        return null;
      }
      return new Coordinate(lonValue, latValue);
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
        Debug.WriteLine("Requenst {0} not found a building", request);
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
        string relationLink = string.Format("http://polygons.openstreetmap.fr/get_geojson.py?id={0}", relationId);
        string geoJsonPoligon = WebHelper.DownloadString(relationLink).Replace('"', '\'');
        GeometryCollection geometryCollection = JsonConvert.DeserializeObject<GeometryCollection>(geoJsonPoligon,
            new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), StringEscapeHandling = StringEscapeHandling.EscapeHtml });
        return geometryCollection.First(geometry => geometry is IMultiPolygon);
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
        string.Format("http://nominatim.openstreetmap.org/reverse.php?format=xml&lat={0}&lon={1}&zoom=18", coord.Y.ToString(CultureInfo.InvariantCulture),
          coord.X.ToString(CultureInfo.InvariantCulture));
      return nominatimRequest;
    }
  }
}
