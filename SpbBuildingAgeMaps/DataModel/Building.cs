using SQLite;

namespace SpbBuildingAgeMaps.DataModel
{
  class Building
  {
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string RawAddress { get; set; }

    public string BuildingType { get; set; }

    public string District { get; set; }

    [Indexed]
    public int BuildYear { get; set; }
  }
}
