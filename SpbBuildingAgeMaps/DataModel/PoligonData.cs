using System;
using System.Collections.Generic;
using System.Text;

namespace SpbBuildingAgeMaps.DataModel
{
  class PoligonData
  {
    public int PoligonDataId { get; set; }

    public int CoordDataId { get; set; }

    public CoordData CoordData { get; set; }

    public string Source { get; set; }

    public byte[] GeometryData { get; set; } 
  }
}
