using System.Text;
using GeoAPI.Geometries;

namespace SpbBuildingAgeMaps
{
  class Building
  {
    public readonly int Id;
    public readonly string RawAddress;
    private string BuildingType;
    private string District;
    public readonly int BuildYear;

    private Coordinate coord;
    private GeoHelper.OsmObject osmObject;

    private Building(int id, string rawAddress, string buildingType, string district, int buildYear)
    {
      this.Id = id;
      this.RawAddress = rawAddress;
      this.BuildingType = buildingType;
      this.District = district;
      this.BuildYear = buildYear;
    }

    public Building(string rawAddress, int buildYear, Coordinate coord)
      : this(0, rawAddress, null, null, buildYear)
    {
      this.coord = coord;
    }

    public static Building Parse(string paramsLine)
    {
      var buildingParams = paramsLine.Split('\t');
      int index, buildYear;
      if (buildingParams.Length < 5 || !int.TryParse(buildingParams[0], out index) || !int.TryParse(buildingParams[4], out buildYear))
      {
        return null;
      }
      return new Building(index, buildingParams[1], buildingParams[2], buildingParams[3], buildYear);
    }

    public GeoHelper.OsmObject OsmObject
    {
      get
      {
        if (osmObject == null)
        {
          Coordinate coord = Coords;
          if (coord != null)
          {
            osmObject = GeoHelper.OsmObject.GetByCoord(coord);
          }
        }
        return osmObject;
      }
    }

    public Coordinate Coords =>
      coord ?? (coord = GeoHelper.GetYandexCoordOfAddress(RawAddress));

    public IGeometry GetPoligone(IGeometryFactory geometryFactory)
    {
      return OsmObject?.GetPoligone(geometryFactory);
    }
  }
}