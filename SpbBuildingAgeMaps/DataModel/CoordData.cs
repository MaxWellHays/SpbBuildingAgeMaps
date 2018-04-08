using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using GeoAPI.Geometries;

namespace SpbBuildingAgeMaps.DataModel
{
  class CoordData
  {
    public int CoordDataId { get; set; }

    public int BuildingId { get; set; }

    public Building Building { get; set; }

    public string Source { get; set; }

    [NotMapped]
    public Coordinate Coordinate { get; set; }

    public double? X
    {
      get => (Coordinate?.X).NullInsteadNan();
      set
      {
        if (Coordinate == null)
        {
          Coordinate = new Coordinate();
        }
        Coordinate.X = value ?? double.NaN;
      }
    }

    public double? Y
    {
      get => (Coordinate?.Y).NullInsteadNan();
      set
      {
        if (Coordinate == null)
        {
          Coordinate = new Coordinate();
        }
        Coordinate.Y = value ?? double.NaN;
      }
    }

    public double? Z
    {
      get => (Coordinate?.Z).NullInsteadNan();
      set
      {
        if (Coordinate == null)
        {
          Coordinate = new Coordinate();
        }
        Coordinate.Z = value ?? double.NaN;
      }
    }

    public List<ReverseGeocodeObject> ReverseGeocodeObjects { get; set; }
  }
}
