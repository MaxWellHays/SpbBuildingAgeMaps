using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SpbBuildingAgeMaps.DataModel;

namespace SpbBuildingAgeMaps
{
  static class OsmObjectHelper
  {
    public static async Task<OsmObject> GetByCoordAsync(CoordData coordData)
    {
      var nominatimResult = await GetOsmObjectFromNominatimApiAsync(coordData).ConfigureAwait(false);
      if (nominatimResult != null)
      {
        return nominatimResult;
      }
      return await GetOsmObjectFromOverpassApiAsync(coordData).ConfigureAwait(false);
    }

    #region OverpassApi
    private static string GetOverpassApiRequest(Coordinate coordinate, double radius)
    {
      var latitude = coordinate.Y;
      var longitude = coordinate.X;
      var dataPart = $"[out:xml]; (way[\"building\"](around:{radius}, {latitude}, {longitude});relation[\"building\"](around:{radius}, {latitude}, {longitude});); out;".Replace(" ", "%20");
      return "http://overpass-api.de/api/interpreter?data=" + dataPart;
    }

    public static async Task<OsmObject> GetOsmObjectFromOverpassApiAsync(CoordData coordData)
    {
      try
      {
        for (int i = 1; i <= 3; i++)
        {
          var request = GetOverpassApiRequest(coordData.Coordinate, 5 * i * i);
          var document = await XmlHelper.LoadXDocumentAsync(request).ConfigureAwait(false);
          var osmElement = document.Element("osm");
          foreach (var xElement in osmElement.Descendants())
          {
            if ((xElement.Name.LocalName == "way" || xElement.Name.LocalName == "relation")
                && xElement.Attribute("id") != null)
            {
              var type = (OsmObjectType)Enum.Parse(typeof(OsmObjectType), xElement.Name.LocalName, true);
              var osmObjectId = int.Parse(xElement.Attribute("id").Value);
              return new OsmObject
              {
                CoordData = coordData,
                CoordDataId = coordData.CoordDataId,
                ExternalOsmObjectId = osmObjectId,
                Type = type,
                Source = "OverpassApi"
              };
            }
          }
        }
        return null;
      }
      catch (WebException exception)
      {
        Console.WriteLine($"Web exception during downloading data from overpass api: {exception}");
        return null;
      }
    }
    #endregion

    #region NominatimApi
    public static string GetNominatimRequest(Coordinate coord)
    {
      string nominatimRequest =
        string.Format("http://nominatim.openstreetmap.org/reverse.php?format=xml&lat={0}&lon={1}&zoom=18&addressdetails=0", coord.Y.ToString(CultureInfo.InvariantCulture),
          coord.X.ToString(CultureInfo.InvariantCulture));
      return nominatimRequest;
    }

    private static async Task<OsmObject> GetOsmObjectFromNominatimApiAsync(CoordData coordData)
    {
      string nominatimRequest = GetNominatimRequest(coordData.Coordinate);
      var nominatimResponseString = await WebHelper.DownloadStringAsync(nominatimRequest).ConfigureAwait(false);
      XDocument doc = XDocument.Parse(nominatimResponseString);

      var element = doc.Element("reversegeocode")?.Element("result");
      if (element == null)
      {
        return null;
      }

      OsmObjectType osmType = (OsmObjectType)Enum.Parse(typeof(OsmObjectType), element.Attribute("osm_type")?.Value, true);
      if (osmType == OsmObjectType.Node)
      {
        Console.WriteLine("Node result from Nominatim. Skiped");
        return null;
      }

      int osmId = int.Parse(element.Attribute("osm_id")?.Value);
      return new OsmObject
      {
        ExternalOsmObjectId = osmId,
        Type = osmType,
        CoordDataId = coordData.CoordDataId,
        CoordData = coordData,
        Source = "NominatimApi",
      };
    }
    #endregion

    public static IGeometry GetPoligone(OsmObject osmObject, IGeometryFactory geometryFactory)
    {
      switch (osmObject.Type)
      {
        case OsmObjectType.Way:
          return GetPoligonOfWay(osmObject.ExternalOsmObjectId, geometryFactory);
        case OsmObjectType.Relation:
          return GetMultypoligonOfRelation(osmObject.ExternalOsmObjectId, geometryFactory);
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
}
