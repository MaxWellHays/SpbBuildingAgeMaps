using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

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
                    BuildingId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BuildYear = table.Column<int>(type: "INTEGER", nullable: false),
                    BuildingType = table.Column<string>(type: "TEXT", nullable: true),
                    District = table.Column<string>(type: "TEXT", nullable: true),
                    RawAddress = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.BuildingId);
                });

            migrationBuilder.CreateTable(
                name: "CoordsData",
                columns: table => new
                {
                    CoordDataId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BuildingId = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    X = table.Column<double>(type: "REAL", nullable: true),
                    Y = table.Column<double>(type: "REAL", nullable: true),
                    Z = table.Column<double>(type: "REAL", nullable: true)
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
                name: "OsmObjects",
                columns: table => new
                {
                    OsmObjectId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CoordDataId = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalOsmObjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OsmObjects", x => x.OsmObjectId);
                    table.ForeignKey(
                        name: "FK_OsmObjects_CoordsData_CoordDataId",
                        column: x => x.CoordDataId,
                        principalTable: "CoordsData",
                        principalColumn: "CoordDataId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoordsData_BuildingId",
                table: "CoordsData",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_OsmObjects_CoordDataId",
                table: "OsmObjects",
                column: "CoordDataId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OsmObjects");

            migrationBuilder.DropTable(
                name: "CoordsData");

            migrationBuilder.DropTable(
                name: "Buildings");
        }
    }
}
