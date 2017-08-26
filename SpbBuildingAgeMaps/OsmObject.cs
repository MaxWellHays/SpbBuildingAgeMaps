using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SpbBuildingAgeMaps
{
  public class OsmObject
  {
    public int id;
    public OsmObjectType type;

    public static OsmObject GetByCoord(Coordinate coord)
    {
      string nominatimRequest = GeoHelper.GetNominatimRequest(coord);
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
        case OsmObjectType.Way:
          return GetPoligonOfWay(id, geometryFactory);
        case OsmObjectType.Relation:
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
          .Select(attribute => GeoHelper.GetOsmNodeCoord(attribute.Value))
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
}
