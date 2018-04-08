using Microsoft.EntityFrameworkCore.Migrations;

namespace SpbBuildingAgeMaps.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Buildings",
                columns: table => new
                {
                    BuildingId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BuildYear = table.Column<int>(nullable: false),
                    BuildingType = table.Column<string>(nullable: true),
                    District = table.Column<string>(nullable: true),
                    RawAddress = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.BuildingId);
                });

            migrationBuilder.CreateTable(
                name: "OsmObjects",
                columns: table => new
                {
                    OsmObjectId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GeometryData = table.Column<byte[]>(nullable: true),
                    Source = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OsmObjects", x => x.OsmObjectId);
                });

            migrationBuilder.CreateTable(
                name: "CoordsData",
                columns: table => new
                {
                    CoordDataId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BuildingId = table.Column<int>(nullable: false),
                    Source = table.Column<string>(nullable: true),
                    X = table.Column<double>(nullable: true),
                    Y = table.Column<double>(nullable: true),
                    Z = table.Column<double>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoordsData", x => x.CoordDataId);
                    table.ForeignKey(
                        name: "FK_CoordsData_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalTable: "Buildings",
                        principalColumn: "BuildingId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReverseGeocodeObjects",
                columns: table => new
                {
                    ReverseGeocodeObjectId = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CoordDataId = table.Column<int>(nullable: false),
                    OsmObjectId = table.Column<int>(nullable: false),
                    Source = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReverseGeocodeObjects", x => x.ReverseGeocodeObjectId);
                    table.ForeignKey(
                        name: "FK_ReverseGeocodeObjects_CoordsData_CoordDataId",
                        column: x => x.CoordDataId,
                        principalTable: "CoordsData",
                        principalColumn: "CoordDataId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReverseGeocodeObjects_OsmObjects_OsmObjectId",
                        column: x => x.OsmObjectId,
                        principalTable: "OsmObjects",
                        principalColumn: "OsmObjectId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoordsData_BuildingId",
                table: "CoordsData",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_ReverseGeocodeObjects_CoordDataId",
                table: "ReverseGeocodeObjects",
                column: "CoordDataId");

            migrationBuilder.CreateIndex(
                name: "IX_ReverseGeocodeObjects_OsmObjectId",
                table: "ReverseGeocodeObjects",
                column: "OsmObjectId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReverseGeocodeObjects");

            migrationBuilder.DropTable(
                name: "CoordsData");

            migrationBuilder.DropTable(
                name: "OsmObjects");

            migrationBuilder.DropTable(
                name: "Buildings");
        }
    }
}
