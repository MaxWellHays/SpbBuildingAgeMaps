using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace SpbBuildingAgeMaps.DataModel
{
  class BuildingContext : DbContext
  {
    public DbSet<BuildingInfo> BuildingInfos { get; set; }
    public DbSet<BuildingInfoWithLocation> BuildingInfoWithLocations { get; set; }
    public DbSet<BuildingInfoWithPoligon> BuildingInfoWithPoligons { get; set; }

    public static string DataDbFilePath
    {
      get
      {
        var projectFolder = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory).Nest(Path.GetDirectoryName, 3);
        return Path.Combine(projectFolder, "data.db");
      }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseSqlite($"Data Source={DataDbFilePath}");
    }
  }
}
