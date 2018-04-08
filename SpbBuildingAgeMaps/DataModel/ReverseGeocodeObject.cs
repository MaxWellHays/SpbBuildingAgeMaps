namespace SpbBuildingAgeMaps.DataModel
{
  class ReverseGeocodeObject
  {
    public int ReverseGeocodeObjectId { get; set; }

    public int OsmObjectId { get; set; }

    public OsmObject OsmObject { get; set; }

    public OsmObjectType Type { get; set; }

    public int CoordDataId { get; set; }

    public CoordData CoordData { get; set; }

    public string Source { get; set; }
  }

  public enum OsmObjectType
  {
    Way,
    Relation,
    Node
  }
}
