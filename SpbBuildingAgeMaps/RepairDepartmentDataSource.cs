using System.IO;
using System.Collections.Generic;
using SpbBuildingAgeMaps.DataModel;
using SpbBuildingAgeMaps.Properties;

namespace SpbBuildingAgeMaps
{
  static class RepairDepartmentDataSource
  {
    public static IEnumerable<Building> GetBuildings()
    {
      using (StringReader reader = new StringReader(Resources.SpbBuildingsAge))
      {
        do
        {
          var line = reader.ReadLine();
          if (line == null)
          {
            yield break;
          }
          Building building = ParseBuildingFromCsv(line);
          if (building != null)
          {
            yield return building;
          }
        } while (true);
      }
    }

    private static Building ParseBuildingFromCsv(string paramsLine)
    {
      var buildingParams = paramsLine.Split('\t');
      int index, buildYear;
      if (buildingParams.Length < 5 || !int.TryParse(buildingParams[0], out index) || !int.TryParse(buildingParams[4], out buildYear))
      {
        return null;
      }
      return new Building
      {
        BuildingId = index,
        BuildYear = buildYear,
        RawAddress = buildingParams[1],
        BuildingType = buildingParams[2],
        District = buildingParams[3]
      };
    }
  }
}
