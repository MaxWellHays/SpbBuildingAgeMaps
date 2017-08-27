using System.Collections.Generic;

namespace SpbBuildingAgeMaps.DataModel
{
  class Building
  {
    public int BuildingId { get; set; }

    public string RawAddress { get; set; }

    public string BuildingType { get; set; }

    public string District { get; set; }

    public int BuildYear { get; set; }

    public List<CoordData> CoordsData { get; set; }
  }
}
