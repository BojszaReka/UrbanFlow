using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Urbanflow.Migrations
{
    /// <inheritdoc />
    public partial class SmallFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GtfsFeeds_GtfsSources_GtfsSourceId",
                table: "GtfsFeeds");

            migrationBuilder.DropTable(
                name: "GtfsSources");

            migrationBuilder.DropIndex(
                name: "IX_GtfsFeeds_GtfsSourceId",
                table: "GtfsFeeds");

            migrationBuilder.DropColumn(
                name: "GtfsSourceId",
                table: "GtfsFeeds");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GtfsSourceId",
                table: "GtfsFeeds",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "GtfsSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CityName = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    GtfsName = table.Column<string>(type: "TEXT", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceUrl = table.Column<string>(type: "TEXT", nullable: false),
                    StringId = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GtfsSources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GtfsFeeds_GtfsSourceId",
                table: "GtfsFeeds",
                column: "GtfsSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_GtfsFeeds_GtfsSources_GtfsSourceId",
                table: "GtfsFeeds",
                column: "GtfsSourceId",
                principalTable: "GtfsSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
