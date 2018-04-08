using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using GeoAPI.Geometries;
using JetBrains.Annotations;
using NetTopologySuite.Geometries;
using SpbBuildingAgeMaps.DataModel;

namespace SpbBuildingAgeMaps
{
  static class OsmObjectHelper
  {
    public static async Task<ReverseGeocodeObject> GetReverseGeocodeObjectByCoordAsync(CoordData coordData)
    {
      var nominatimResult = await GetReverseGeocodeObjectFromNominatimApiAsync(coordData).ConfigureAwait(false);
      if (nominatimResult != null)
      {
        return nominatimResult;
      }
      return await GetReverseGeocodeObjectFromOverpassApiAsync(coordData).ConfigureAwait(false);
    }

    #region OverpassApi
    private static string GetOverpassApiRequest(Coordinate coordinate, double radius)
    {
      var latitude = coordinate.Y;
      var longitude = coordinate.X;
      var dataPart = $"[out:xml]; (way[\"building\"](around:{radius}, {latitude}, {longitude});relation[\"building\"](around:{radius}, {latitude}, {longitude});); out;".Replace(" ", "%20");
      return "http://overpass-api.de/api/interpreter?data=" + dataPart;
    }

    public static async Task<ReverseGeocodeObject> GetReverseGeocodeObjectFromOverpassApiAsync(CoordData coordData)
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
              return new ReverseGeocodeObject
              {
                CoordData = coordData,
                CoordDataId = coordData.CoordDataId,
                OsmObjectId = osmObjectId,
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

    private static async Task<ReverseGeocodeObject> GetReverseGeocodeObjectFromNominatimApiAsync(CoordData coordData)
    {
      string nominatimRequest = GetNominatimRequest(coordData.Coordinate);
      XDocument doc = await XmlHelper.LoadXDocumentAsync(nominatimRequest).ConfigureAwait(false);

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
      return new ReverseGeocodeObject
      {
        OsmObjectId = osmId,
        Type = osmType,
        CoordDataId = coordData.CoordDataId,
        CoordData = coordData,
        Source = "NominatimApi",
      };
    }
    #endregion

    [ItemCanBeNull]
    public static async Task<IGeometry> GetPoligoneAsync(int osmObjectId, OsmObjectType osmObjectType, IGeometryFactory geometryFactory)
    {
      switch (osmObjectType)
      {
        case OsmObjectType.Way:
          return await GetPoligonOfWayAsync(osmObjectId, geometryFactory).ConfigureAwait(false);
        case OsmObjectType.Relation:
          return await GetMultypoligonOfRelationAsync(osmObjectId, geometryFactory).ConfigureAwait(false);
        default:
          throw new NotImplementedException();
      }
    }

    public static async Task<IPolygon> GetPoligonOfWayAsync(int wayId, IGeometryFactory geometryFactory)
    {
      string wayLink = string.Format("https://www.openstreetmap.org/api/0.6/way/{0}", wayId);
      XDocument wayObject = await XmlHelper.LoadXDocumentAsync(wayLink).ConfigureAwait(false);
      Task<Coordinate>[] nodeTasks = wayObject.Descendants("nd")
        .Attributes("ref")
        .Select(attribute => GeoHelper.GetOsmNodeCoordAsync(attribute.Value))
        .ToArray();

      Coordinate[] coordinates = await Task.WhenAll(nodeTasks).ConfigureAwait(false);

      await ConsoleHelper.ColorWriteLineAsync(ConsoleColor.Magenta, "Received way with id {0}", wayId).ConfigureAwait(false);

      if (coordinates.Length > 2)
      {
        var lineString = geometryFactory.CreateLineString(coordinates);
        if (lineString.IsClosed)
        {
          return geometryFactory.CreatePolygon(coordinates);
        }
      }

      return Polygon.Empty;
    }

    [ItemCanBeNull]
    public static async Task<IGeometry> GetMultypoligonOfRelationAsync(int relationId, IGeometryFactory geometryFactory)
    {
      string relationLink = string.Format("https://www.openstreetmap.org/api/0.6/relation/{0}", relationId);
      XDocument wayObject = await XmlHelper.LoadXDocumentAsync(relationLink).ConfigureAwait(false);

      Task<IPolygon>[] tasks = wayObject.Descendants("member").Where(element => element.Attribute("type").Value.Equals("way"))
        .Select(wayElement => GetPoligonOfWayAsync(int.Parse(wayElement.Attribute("ref").Value), geometryFactory)).ToArray();

      IPolygon[] poligons = await Task.WhenAll(tasks).ConfigureAwait(false);

      await ConsoleHelper.ColorWriteLineAsync(ConsoleColor.Red, "Received relation with id {0}", relationId).ConfigureAwait(false);

      return geometryFactory.CreateMultiPolygon(poligons);
    }
  }
}
