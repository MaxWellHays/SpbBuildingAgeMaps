using GeoAPI.Geometries;
using SQLite;

namespace SpbBuildingAgeMaps.DataModel
{
  class CoordData
  {
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int BuildingId { get; set; }

    public string Source { get; set; }

    [Ignore]
    public Coordinate Coordinate { get; set; }

    public double X
    {
      get => Coordinate?.X ?? 0;
      set
      {
        if (Coordinate == null)
        {
          Coordinate = new Coordinate();
        }
        Coordinate.X = value;
      }
    }

    public double Y
    {
      get => Coordinate?.Y ?? 0;
      set
      {
        if (Coordinate == null)
        {
          Coordinate = new Coordinate();
        }
        Coordinate.Y = value;
      }
    }

    public double Z
    {
      get => Coordinate?.Z ?? 0;
      set
      {
        if (Coordinate == null)
        {
          Coordinate = new Coordinate();
        }
        Coordinate.Z = value;
      }
    }
  }
}
