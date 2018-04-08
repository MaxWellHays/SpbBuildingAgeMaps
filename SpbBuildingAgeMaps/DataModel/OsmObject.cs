using System.Collections.Generic;

namespace SpbBuildingAgeMaps.DataModel
{
  class OsmObject
  {
    public int OsmObjectId { get; set; }

    public List<ReverseGeocodeObject> ReverseGeocodeObjects { get; set; }

    public string Source { get; set; }

    public byte[] GeometryData { get; set; } 
  }
}
