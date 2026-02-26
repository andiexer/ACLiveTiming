using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Devlabs.AcTiming.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Car",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Skin = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Car", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Driver",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Guid = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Team = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Driver", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Tracks",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Config = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracks", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "Session",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerName = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    TrackId = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeLimitMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    LapLimit = table.Column<int>(type: "INTEGER", nullable: false),
                    AmbientTemp = table.Column<int>(type: "INTEGER", nullable: false),
                    RoadTemp = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Session", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Session_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "TrackConfigs",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrackId = table.Column<int>(type: "INTEGER", nullable: false),
                    PitLane = table.Column<string>(type: "TEXT", nullable: true),
                    SpeedTraps = table.Column<string>(type: "TEXT", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackConfigs_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "DriverSession",
                columns: table => new
                {
                    DriversId = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionsId = table.Column<int>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverSession", x => new { x.DriversId, x.SessionsId });
                    table.ForeignKey(
                        name: "FK_DriverSession_Driver_DriversId",
                        column: x => x.DriversId,
                        principalTable: "Driver",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_DriverSession_Session_SessionsId",
                        column: x => x.SessionsId,
                        principalTable: "Session",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Lap",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    DriverId = table.Column<int>(type: "INTEGER", nullable: false),
                    CarId = table.Column<int>(type: "INTEGER", nullable: false),
                    TrackId = table.Column<int>(type: "INTEGER", nullable: false),
                    LapTimeMs = table.Column<int>(type: "INTEGER", nullable: false),
                    SectorTimesMs = table.Column<string>(type: "TEXT", nullable: false),
                    Cuts = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lap", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lap_Car_CarId",
                        column: x => x.CarId,
                        principalTable: "Car",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_Lap_Driver_DriverId",
                        column: x => x.DriverId,
                        principalTable: "Driver",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_Lap_Session_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Session",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_Lap_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_DriverSession_SessionsId",
                table: "DriverSession",
                column: "SessionsId"
            );

            migrationBuilder.CreateIndex(name: "IX_Lap_CarId", table: "Lap", column: "CarId");

            migrationBuilder.CreateIndex(name: "IX_Lap_DriverId", table: "Lap", column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Lap_SessionId",
                table: "Lap",
                column: "SessionId"
            );

            migrationBuilder.CreateIndex(name: "IX_Lap_TrackId", table: "Lap", column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_Session_TrackId",
                table: "Session",
                column: "TrackId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_TrackConfigs_TrackId",
                table: "TrackConfigs",
                column: "TrackId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Tracks_Name_Config",
                table: "Tracks",
                columns: new[] { "Name", "Config" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DriverSession");

            migrationBuilder.DropTable(name: "Lap");

            migrationBuilder.DropTable(name: "TrackConfigs");

            migrationBuilder.DropTable(name: "Car");

            migrationBuilder.DropTable(name: "Driver");

            migrationBuilder.DropTable(name: "Session");

            migrationBuilder.DropTable(name: "Tracks");
        }
    }
}
