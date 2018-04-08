using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using GeoAPI.Geometries;
using SpbBuildingAgeMaps.DataModel;

namespace SpbBuildingAgeMaps
{
  static partial class GeoHelper
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

    public static async Task<Coordinate> GetYandexCoordOfAddressAsync(string rawAddress)
    {
      foreach (string request in GetYandexRequests(rawAddress))
      {
        XDocument doc = await XmlHelper.LoadClearXDocumentAsync(request).ConfigureAwait(false);
        foreach (XElement geoObject in doc.Descendants("GeoObject"))
        {
          string type = geoObject.Descendants("kind").First().Value;
          if (type == "house")
          {
            var coord = ParseCoord(geoObject.Descendants("pos").First().Value);
            if (coord != null)
            {
              return coord;
            }
          }
        }
      }
      Debug.WriteLine($"Not found address \"{rawAddress}\"");
      return null;
    }
    
    public static async Task<Coordinate> GetOsmNodeCoordAsync(string nodeId)
    {
      string nodeLink = string.Format("https://www.openstreetmap.org/api/0.6/node/{0}", nodeId);
      var nodeDoc = await XmlHelper.LoadXDocumentAsync(nodeLink).ConfigureAwait(false);
      string latValue = nodeDoc.Descendants("node").Attributes("lat").First().Value;
      double lat = double.Parse(latValue.Trim('"').Replace(".", ","));
      string lonValue = nodeDoc.Descendants("node").Attributes("lon").First().Value;
      double lon = double.Parse(lonValue.Trim('"').Replace(".", ","));
      await ConsoleHelper.ColorWriteLineAsync(ConsoleColor.Yellow, "Received coord ({1}; {2}) with id {0}", nodeId, lon, lat).ConfigureAwait(false);
      return new Coordinate(lon, lat);
    }

    //public static IGeometry GetPoligone(this Building building, IGeometryFactory geometryFactory)
    //{
    //  return OsmObjectHelper.GetByCoord(building.GetCoords())?.GetPoligone(geometryFactory);
    //}

    public static double? NullInsteadNan(this double? value)
    {
      return value?.NullInsteadNan();
    }

    public static double? NullInsteadNan(this double value)
    {
      if (double.IsNaN(value))
      {
        return null;
      }
      return value;
    }
  }
}
