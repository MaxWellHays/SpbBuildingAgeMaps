using System;
using System.Collections.Generic;
using System.Text;

namespace SpbBuildingAgeMaps.DataModel
{
  class OsmObject
  {
    public int OsmObjectId { get; set; }

    public int ExternalOsmObjectId { get; set; }

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
