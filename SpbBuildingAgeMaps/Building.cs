using System.Text;
using GeoAPI.Geometries;

namespace SpbBuildingAgeMaps
{
  class Building
  {
    public readonly int index;
    public readonly string rawAddress;
    private string type;
    private string district;
    public readonly int buildYear;
    private Coordinate coord;
    private GeoHelper.OsmObject osmObject;

    private Building(int index, string rawAddress, string type, string district, int buildYear)
    {
      this.index = index;
      this.rawAddress = rawAddress;
      this.type = type;
      this.district = district;
      this.buildYear = buildYear;
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
      coord ?? (coord = GeoHelper.GetYandexCoordOfAddress(rawAddress));

    public IGeometry GetPoligone(IGeometryFactory geometryFactory)
    {
      return OsmObject?.GetPoligone(geometryFactory);
    }
  }
}