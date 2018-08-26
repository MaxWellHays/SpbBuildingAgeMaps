using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ExcelDataReader;
using SpbBuildingAgeMaps.DataModel;

namespace SpbBuildingAgeMaps.DataProviders
{
  internal class OverhaulProgramBuildingsProvider
  {
    private const string OverhaulProgramDataFileName = @"DataCache\OverhaulProgramBuildings.xls";
    private const string OverhaulProgramXlsTableFileUrl = @"https://cdn.fontanka.ru/mm/items/2014/1/20/0059/Remont.xls";

    public async Task<IEnumerable<BuildingInfo>> GetBuildingsAsync()
    {
      if (!File.Exists(OverhaulProgramDataFileName))
      {
        using (FileStream fileStream = new FileStream(OverhaulProgramDataFileName, FileMode.Create))
        using (Stream fileContent = await WebHelper.DownloadFileAsync(OverhaulProgramXlsTableFileUrl))
        {
          await fileContent.CopyToAsync(fileStream);
        }
      }

      return ReadBuildingInfoFromCacheFile();
    }

    private IEnumerable<BuildingInfo> ReadBuildingInfoFromCacheFile()
    {
      using (FileStream stream = File.Open(OverhaulProgramDataFileName, FileMode.Open, FileAccess.Read))
      using (IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream))
      {
        do
        {
          while (reader.Read())
          {
            object firstColumnValue = reader.GetValue(0);
            if (firstColumnValue is int || firstColumnValue is string firstColumnStringValue && int.TryParse(firstColumnStringValue, out _))
            {
              string address = reader.GetString(1);
              string yearValue = reader.GetValue(4)?.ToString();
              if (!int.TryParse(yearValue, out int year))
              {
                continue;
              }

              BuildingInfo buildingInfo = BuildingInfo.Create(address, year);
              yield return buildingInfo;
            }
          }
        } while (reader.NextResult());
      }
    }
  }
}
