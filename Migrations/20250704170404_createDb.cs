using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspnetCoreMvcFull.Migrations
{
    /// <inheritdoc />
    public partial class createDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    ClientID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NumOfBins = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.ClientID);
                });

            migrationBuilder.CreateTable(
                name: "RoutePlan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoutePlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinPlateId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FillLevel = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Zone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true),
                    ClientID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bins_Clients_ClientID",
                        column: x => x.ClientID,
                        principalTable: "Clients",
                        principalColumn: "ClientID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trucks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LicensePlate = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Model = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DriverId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trucks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trucks_Users_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BinReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsIssueReported = table.Column<bool>(type: "bit", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReportedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BinReports_Bins_BinId",
                        column: x => x.BinId,
                        principalTable: "Bins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RouteBins",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderInRoute = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteBins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteBins_Bins_BinId",
                        column: x => x.BinId,
                        principalTable: "Bins",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RouteBins_RoutePlan_RouteId",
                        column: x => x.RouteId,
                        principalTable: "RoutePlan",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TruckId = table.Column<int>(type: "int", nullable: false),
                    CollectorId = table.Column<int>(type: "int", nullable: false),
                    RouteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleStartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduleEndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DayOfWeek = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ActualStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AdminNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RouteCenterLatitude = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    RouteCenterLongitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schedules_RoutePlan_RouteId",
                        column: x => x.RouteId,
                        principalTable: "RoutePlan",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Schedules_Trucks_TruckId",
                        column: x => x.TruckId,
                        principalTable: "Trucks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Schedules_Users_CollectorId",
                        column: x => x.CollectorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BinReportImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinReportImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BinReportImages_BinReports_BinReportId",
                        column: x => x.BinReportId,
                        principalTable: "BinReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectionPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleId = table.Column<int>(type: "int", nullable: false),
                    BinId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderInSchedule = table.Column<int>(type: "int", nullable: false),
                    IsCollected = table.Column<bool>(type: "bit", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,8)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(11,8)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionPoints_Bins_BinId",
                        column: x => x.BinId,
                        principalTable: "Bins",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CollectionPoints_Schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MissedPickups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleId = table.Column<int>(type: "int", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissedPickups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissedPickups_Schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BinId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollectionPointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollectorId = table.Column<int>(type: "int", nullable: false),
                    TruckId = table.Column<int>(type: "int", nullable: false),
                    PickupTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GpsLatitude = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    GpsLongitude = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    BinPlateIdCaptured = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionRecords_Bins_BinId",
                        column: x => x.BinId,
                        principalTable: "Bins",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CollectionRecords_CollectionPoints_CollectionPointId",
                        column: x => x.CollectionPointId,
                        principalTable: "CollectionPoints",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CollectionRecords_Trucks_TruckId",
                        column: x => x.TruckId,
                        principalTable: "Trucks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CollectionRecords_Users_CollectorId",
                        column: x => x.CollectorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CollectionRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Images_CollectionRecords_CollectionRecordId",
                        column: x => x.CollectionRecordId,
                        principalTable: "CollectionRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BinReportImages_BinReportId",
                table: "BinReportImages",
                column: "BinReportId");

            migrationBuilder.CreateIndex(
                name: "IX_BinReports_BinId",
                table: "BinReports",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_Bins_ClientID",
                table: "Bins",
                column: "ClientID");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionPoints_BinId",
                table: "CollectionPoints",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionPoints_ScheduleId",
                table: "CollectionPoints",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionRecords_BinId",
                table: "CollectionRecords",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionRecords_CollectionPointId",
                table: "CollectionRecords",
                column: "CollectionPointId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionRecords_CollectorId",
                table: "CollectionRecords",
                column: "CollectorId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionRecords_TruckId",
                table: "CollectionRecords",
                column: "TruckId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_CollectionRecordId",
                table: "Images",
                column: "CollectionRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MissedPickups_ScheduleId",
                table: "MissedPickups",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteBins_BinId",
                table: "RouteBins",
                column: "BinId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteBins_RouteId",
                table: "RouteBins",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_CollectorId",
                table: "Schedules",
                column: "CollectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_RouteId",
                table: "Schedules",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TruckId",
                table: "Schedules",
                column: "TruckId");

            migrationBuilder.CreateIndex(
                name: "IX_Trucks_DriverId",
                table: "Trucks",
                column: "DriverId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BinReportImages");

            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropTable(
                name: "MissedPickups");

            migrationBuilder.DropTable(
                name: "RouteBins");

            migrationBuilder.DropTable(
                name: "BinReports");

            migrationBuilder.DropTable(
                name: "CollectionRecords");

            migrationBuilder.DropTable(
                name: "CollectionPoints");

            migrationBuilder.DropTable(
                name: "Bins");

            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "RoutePlan");

            migrationBuilder.DropTable(
                name: "Trucks");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
