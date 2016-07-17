using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GeoAPI.Geometries;
using NetTopologySuite;
using NetTopologySuite.Features;
using SpbBuildingAgeMaps.Properties;

namespace SpbBuildingAgeMaps
{
  class Program
  {
    static void Main()
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
          writer.WriteLine("\"{0}\",{1},{2},{3}", building.rawAddress, building.buildYear,
            coord.X.ToString(System.Globalization.CultureInfo.InvariantCulture),
            coord.Y.ToString(System.Globalization.CultureInfo.InvariantCulture));
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
        table.AddAttribute("address", building.rawAddress);
        table.AddAttribute("buildYear", building.buildYear);
        IGeometry buildingPoligon = building.GetPoligone(geomFactory);
        features.Add(new Feature(buildingPoligon, table));
      }
      return features;
    }
  }
}
