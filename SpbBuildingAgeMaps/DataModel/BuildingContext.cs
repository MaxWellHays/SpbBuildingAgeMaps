using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace SpbBuildingAgeMaps.DataModel
{
  class BuildingContext : DbContext
  {
    public DbSet<Building> Buildings { get; set; }
    public DbSet<CoordData> CoordsData { get; set; }

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
