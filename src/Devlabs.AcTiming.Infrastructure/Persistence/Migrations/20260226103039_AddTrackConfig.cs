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
            migrationBuilder.DropTable(name: "TrackConfigs");

            migrationBuilder.DropTable(name: "Tracks");
        }
    }
}
