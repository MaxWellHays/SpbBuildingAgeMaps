using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpbBuildingAgeMaps.DataModel;

namespace SpbBuildingAgeMaps.DataProviders
{
  class BuildingInfoProvider
  {
    public async Task GenerateCityDataFileAsync()
    {
      using (BuildingContext buildingContext = new BuildingContext())
      {
        int buildingInfosCount = await buildingContext.BuildingInfos.CountAsync();
        int buildingsWithPoligonsCount = await buildingContext.BuildingInfoWithPoligons.CountAsync();

        if (buildingsWithPoligonsCount == 0 && buildingInfosCount == 0)
        {
          OverhaulProgramBuildingsProvider overhaulProgramBuildingsProvider = new OverhaulProgramBuildingsProvider();
          List<BuildingInfo> buildingInfos = (await overhaulProgramBuildingsProvider.GetBuildingsAsync()).ToList();
          await buildingContext.BuildingInfos.AddRangeAsync(buildingInfos);
          await buildingContext.SaveChangesAsync();
        }
      }
    }
  }
}
