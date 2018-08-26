using System.Linq;

namespace SpbBuildingAgeMaps.DataModel
{
  class BuildingInfo
  {
    public int BuildingInfoId { get; set; }

    public string Address { get; set; }

    public int BuildYear { get; set; }

    public static BuildingInfo Create(string address, int buildYear)
    {
      string normalizedAddress = AddressHelper.NormalizeAddress(address).First();
      return new BuildingInfo { Address = normalizedAddress, BuildYear = buildYear };
    }
  }
}
