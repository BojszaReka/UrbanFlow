using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Urbanflow.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GtfsFeedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Edges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromNodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ToNodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Edges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeedInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GraphEdges",
                columns: table => new
                {
                    GraphId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EdgeId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraphEdges", x => new { x.GraphId, x.EdgeId });
                });

            migrationBuilder.CreateTable(
                name: "GraphNodes",
                columns: table => new
                {
                    GraphId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NodeId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraphNodes", x => new { x.GraphId, x.NodeId });
                });

            migrationBuilder.CreateTable(
                name: "Graphs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RouteId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Graphs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GtfsSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StringId = table.Column<string>(type: "TEXT", nullable: false),
                    CityName = table.Column<string>(type: "TEXT", nullable: false),
                    SourceUrl = table.Column<string>(type: "TEXT", nullable: false),
                    GtfsName = table.Column<string>(type: "TEXT", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GtfsSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Nodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    StopId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GtfsFeeds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GtfsSourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PublisherName = table.Column<string>(type: "TEXT", nullable: false),
                    PublisherUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Lang = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<string>(type: "TEXT", nullable: false),
                    EndDate = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GtfsFeeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GtfsFeeds_GtfsSources_GtfsSourceId",
                        column: x => x.GtfsSourceId,
                        principalTable: "GtfsSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Agencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GtfsFeedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Agency_Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    URL = table.Column<string>(type: "TEXT", nullable: false),
                    Timezone = table.Column<string>(type: "TEXT", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: false),
                    FareURL = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agencies_GtfsFeeds_GtfsFeedId",
                        column: x => x.GtfsFeedId,
                        principalTable: "GtfsFeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalendarDates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GtfsFeedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExceptionType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarDates_GtfsFeeds_GtfsFeedId",
                        column: x => x.GtfsFeedId,
                        principalTable: "GtfsFeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Calendars",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GtfsFeedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<string>(type: "TEXT", nullable: true),
                    Mask = table.Column<byte>(type: "INTEGER", nullable: false),
                    Monday = table.Column<bool>(type: "INTEGER", nullable: false),
                    Tuesday = table.Column<bool>(type: "INTEGER", nullable: false),
                    Wednesday = table.Column<bool>(type: "INTEGER", nullable: false),
                    Thursday = table.Column<bool>(type: "INTEGER", nullable: false),
                    Friday = table.Column<bool>(type: "INTEGER", nullable: false),
                    Saturday = table.Column<bool>(type: "INTEGER", nullable: false),
                    Sunday = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calendars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calendars_GtfsFeeds_GtfsFeedId",
                        column: x => x.GtfsFeedId,
                        principalTable: "GtfsFeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GtfsFeedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RouteId = table.Column<string>(type: "TEXT", nullable: false),
                    AgencyId = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    LongName = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<int>(type: "INTEGER", nullable: true),
                    TextColor = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routes_GtfsFeeds_GtfsFeedId",
                        column: x => x.GtfsFeedId,
                        principalTable: "GtfsFeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Shapes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GtfsFeedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ShapeId = table.Column<string>(type: "TEXT", nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Sequence = table.Column<uint>(type: "INTEGER", nullable: false),
                    DistanceTravelled = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shapes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shapes_GtfsFeeds_GtfsFeedId",
                        column: x => x.GtfsFeedId,
                        principalTable: "GtfsFeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GtfsFeedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NodeType = table.Column<int>(type: "INTEGER", nullable: false),
                    StopId = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Zone = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    LocationType = table.Column<int>(type: "INTEGER", nullable: true),
                    ParentStation = table.Column<string>(type: "TEXT", nullable: false),
                    Timezone = table.Column<string>(type: "TEXT", nullable: false),
                    WheelchairBoarding = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stops_GtfsFeeds_GtfsFeedId",
                        column: x => x.GtfsFeedId,
                        principalTable: "GtfsFeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StopTimes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GtfsFeedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TripId = table.Column<string>(type: "TEXT", nullable: false),
                    ArrivalTime = table.Column<string>(type: "TEXT", nullable: false),
                    DepartureTime = table.Column<string>(type: "TEXT", nullable: false),
                    StopId = table.Column<string>(type: "TEXT", nullable: false),
                    StopSequence = table.Column<uint>(type: "INTEGER", nullable: false),
                    StopHeadsign = table.Column<string>(type: "TEXT", nullable: false),
                    PickupType = table.Column<int>(type: "INTEGER", nullable: true),
                    DropOffType = table.Column<int>(type: "INTEGER", nullable: true),
                    ShapeDistTravelled = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StopTimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StopTimes_GtfsFeeds_GtfsFeedId",
                        column: x => x.GtfsFeedId,
                        principalTable: "GtfsFeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GtfsFeedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TripId = table.Column<string>(type: "TEXT", nullable: false),
                    RouteId = table.Column<string>(type: "TEXT", nullable: false),
                    ServiceId = table.Column<string>(type: "TEXT", nullable: false),
                    Headsign = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    Direction = table.Column<int>(type: "INTEGER", nullable: true),
                    BlockId = table.Column<string>(type: "TEXT", nullable: false),
                    ShapeId = table.Column<string>(type: "TEXT", nullable: false),
                    AccessibilityType = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trips_GtfsFeeds_GtfsFeedId",
                        column: x => x.GtfsFeedId,
                        principalTable: "GtfsFeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workflows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GtfsFeedId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workflows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workflows_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Workflows_GtfsFeeds_GtfsFeedId",
                        column: x => x.GtfsFeedId,
                        principalTable: "GtfsFeeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agencies_GtfsFeedId",
                table: "Agencies",
                column: "GtfsFeedId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarDates_GtfsFeedId",
                table: "CalendarDates",
                column: "GtfsFeedId");

            migrationBuilder.CreateIndex(
                name: "IX_Calendars_GtfsFeedId",
                table: "Calendars",
                column: "GtfsFeedId");

            migrationBuilder.CreateIndex(
                name: "IX_GtfsFeeds_GtfsSourceId",
                table: "GtfsFeeds",
                column: "GtfsSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_GtfsFeedId",
                table: "Routes",
                column: "GtfsFeedId");

            migrationBuilder.CreateIndex(
                name: "IX_Shapes_GtfsFeedId",
                table: "Shapes",
                column: "GtfsFeedId");

            migrationBuilder.CreateIndex(
                name: "IX_Stops_GtfsFeedId",
                table: "Stops",
                column: "GtfsFeedId");

            migrationBuilder.CreateIndex(
                name: "IX_StopTimes_GtfsFeedId",
                table: "StopTimes",
                column: "GtfsFeedId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_GtfsFeedId",
                table: "Trips",
                column: "GtfsFeedId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_CityId",
                table: "Workflows",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Workflows_GtfsFeedId",
                table: "Workflows",
                column: "GtfsFeedId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Agencies");

            migrationBuilder.DropTable(
                name: "CalendarDates");

            migrationBuilder.DropTable(
                name: "Calendars");

            migrationBuilder.DropTable(
                name: "Edges");

            migrationBuilder.DropTable(
                name: "FeedInfos");

            migrationBuilder.DropTable(
                name: "GraphEdges");

            migrationBuilder.DropTable(
                name: "GraphNodes");

            migrationBuilder.DropTable(
                name: "Graphs");

            migrationBuilder.DropTable(
                name: "Nodes");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "Shapes");

            migrationBuilder.DropTable(
                name: "Stops");

            migrationBuilder.DropTable(
                name: "StopTimes");

            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.DropTable(
                name: "Workflows");

            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "GtfsFeeds");

            migrationBuilder.DropTable(
                name: "GtfsSources");
        }
    }
}
