using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Urbanflow.Migrations
{
    /// <inheritdoc />
    public partial class Genome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Genomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GenerationID = table.Column<int>(type: "INTEGER", nullable: false),
                    FitnessValue = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genomes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GenomeRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GenomeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OnStartTime = table.Column<int>(type: "INTEGER", nullable: false),
                    BackStartTime = table.Column<int>(type: "INTEGER", nullable: false),
                    Headway = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenomeRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GenomeRoutes_Genomes_GenomeId",
                        column: x => x.GenomeId,
                        principalTable: "Genomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouteStops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GenomeRouteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StopId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Direction = table.Column<int>(type: "INTEGER", nullable: false),
                    StopSequence = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteStops_GenomeRoutes_GenomeRouteId",
                        column: x => x.GenomeRouteId,
                        principalTable: "GenomeRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GenomeRoutes_GenomeId",
                table: "GenomeRoutes",
                column: "GenomeId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_GenomeRouteId",
                table: "RouteStops",
                column: "GenomeRouteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteStops");

            migrationBuilder.DropTable(
                name: "GenomeRoutes");

            migrationBuilder.DropTable(
                name: "Genomes");
        }
    }
}
