using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Urbanflow.Migrations
{
    /// <inheritdoc />
    public partial class _0319_Changes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workflows_GtfsFeeds_GtfsFeedId",
                table: "Workflows");

            migrationBuilder.DropIndex(
                name: "IX_Workflows_GtfsFeedId",
                table: "Workflows");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Graphs",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Graphs",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Graphs");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "Graphs");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_GtfsFeedId",
                table: "Workflows",
                column: "GtfsFeedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Workflows_GtfsFeeds_GtfsFeedId",
                table: "Workflows",
                column: "GtfsFeedId",
                principalTable: "GtfsFeeds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
