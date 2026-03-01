using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Devlabs.AcTiming.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverSession_Driver_DriversId",
                table: "DriverSession"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_DriverSession_Session_SessionsId",
                table: "DriverSession"
            );

            migrationBuilder.DropForeignKey(name: "FK_Lap_Car_CarId", table: "Lap");

            migrationBuilder.DropForeignKey(name: "FK_Lap_Driver_DriverId", table: "Lap");

            migrationBuilder.DropForeignKey(name: "FK_Lap_Session_SessionId", table: "Lap");

            migrationBuilder.DropForeignKey(name: "FK_Lap_Tracks_TrackId", table: "Lap");

            migrationBuilder.DropForeignKey(name: "FK_Session_Tracks_TrackId", table: "Session");

            migrationBuilder.DropPrimaryKey(name: "PK_Session", table: "Session");

            migrationBuilder.DropPrimaryKey(name: "PK_Lap", table: "Lap");

            migrationBuilder.DropPrimaryKey(name: "PK_Driver", table: "Driver");

            migrationBuilder.DropPrimaryKey(name: "PK_Car", table: "Car");

            migrationBuilder.RenameTable(name: "Session", newName: "Sessions");

            migrationBuilder.RenameTable(name: "Lap", newName: "Laps");

            migrationBuilder.RenameTable(name: "Driver", newName: "Drivers");

            migrationBuilder.RenameTable(name: "Car", newName: "Cars");

            migrationBuilder.RenameIndex(
                name: "IX_Session_TrackId",
                table: "Sessions",
                newName: "IX_Sessions_TrackId"
            );

            migrationBuilder.RenameIndex(
                name: "IX_Lap_TrackId",
                table: "Laps",
                newName: "IX_Laps_TrackId"
            );

            migrationBuilder.RenameIndex(
                name: "IX_Lap_SessionId",
                table: "Laps",
                newName: "IX_Laps_SessionId"
            );

            migrationBuilder.RenameIndex(
                name: "IX_Lap_DriverId",
                table: "Laps",
                newName: "IX_Laps_DriverId"
            );

            migrationBuilder.RenameIndex(
                name: "IX_Lap_CarId",
                table: "Laps",
                newName: "IX_Laps_CarId"
            );

            migrationBuilder.AddColumn<int>(
                name: "ClosedReason",
                table: "Sessions",
                type: "INTEGER",
                nullable: true
            );

            migrationBuilder.AddColumn<bool>(
                name: "IsValid",
                table: "Laps",
                type: "INTEGER",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<int>(
                name: "LapNumber",
                table: "Laps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<float>(
                name: "MaxSpeedKmh",
                table: "Laps",
                type: "REAL",
                nullable: false,
                defaultValue: 0f
            );

            migrationBuilder.AddColumn<byte[]>(
                name: "TelemetrySamples",
                table: "Laps",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]
            );

            migrationBuilder.AddPrimaryKey(name: "PK_Sessions", table: "Sessions", column: "Id");

            migrationBuilder.AddPrimaryKey(name: "PK_Laps", table: "Laps", column: "Id");

            migrationBuilder.AddPrimaryKey(name: "PK_Drivers", table: "Drivers", column: "Id");

            migrationBuilder.AddPrimaryKey(name: "PK_Cars", table: "Cars", column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_EndedAtUtc",
                table: "Sessions",
                column: "EndedAtUtc"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_Guid",
                table: "Drivers",
                column: "Guid",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_Cars_Model_Skin",
                table: "Cars",
                columns: new[] { "Model", "Skin" },
                unique: true
            );

            migrationBuilder.AddForeignKey(
                name: "FK_DriverSession_Drivers_DriversId",
                table: "DriverSession",
                column: "DriversId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_DriverSession_Sessions_SessionsId",
                table: "DriverSession",
                column: "SessionsId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Laps_Cars_CarId",
                table: "Laps",
                column: "CarId",
                principalTable: "Cars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Laps_Drivers_DriverId",
                table: "Laps",
                column: "DriverId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Laps_Sessions_SessionId",
                table: "Laps",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Laps_Tracks_TrackId",
                table: "Laps",
                column: "TrackId",
                principalTable: "Tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Tracks_TrackId",
                table: "Sessions",
                column: "TrackId",
                principalTable: "Tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverSession_Drivers_DriversId",
                table: "DriverSession"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_DriverSession_Sessions_SessionsId",
                table: "DriverSession"
            );

            migrationBuilder.DropForeignKey(name: "FK_Laps_Cars_CarId", table: "Laps");

            migrationBuilder.DropForeignKey(name: "FK_Laps_Drivers_DriverId", table: "Laps");

            migrationBuilder.DropForeignKey(name: "FK_Laps_Sessions_SessionId", table: "Laps");

            migrationBuilder.DropForeignKey(name: "FK_Laps_Tracks_TrackId", table: "Laps");

            migrationBuilder.DropForeignKey(name: "FK_Sessions_Tracks_TrackId", table: "Sessions");

            migrationBuilder.DropPrimaryKey(name: "PK_Sessions", table: "Sessions");

            migrationBuilder.DropIndex(name: "IX_Sessions_EndedAtUtc", table: "Sessions");

            migrationBuilder.DropPrimaryKey(name: "PK_Laps", table: "Laps");

            migrationBuilder.DropPrimaryKey(name: "PK_Drivers", table: "Drivers");

            migrationBuilder.DropIndex(name: "IX_Drivers_Guid", table: "Drivers");

            migrationBuilder.DropPrimaryKey(name: "PK_Cars", table: "Cars");

            migrationBuilder.DropIndex(name: "IX_Cars_Model_Skin", table: "Cars");

            migrationBuilder.DropColumn(name: "ClosedReason", table: "Sessions");

            migrationBuilder.DropColumn(name: "IsValid", table: "Laps");

            migrationBuilder.DropColumn(name: "LapNumber", table: "Laps");

            migrationBuilder.DropColumn(name: "MaxSpeedKmh", table: "Laps");

            migrationBuilder.DropColumn(name: "TelemetrySamples", table: "Laps");

            migrationBuilder.RenameTable(name: "Sessions", newName: "Session");

            migrationBuilder.RenameTable(name: "Laps", newName: "Lap");

            migrationBuilder.RenameTable(name: "Drivers", newName: "Driver");

            migrationBuilder.RenameTable(name: "Cars", newName: "Car");

            migrationBuilder.RenameIndex(
                name: "IX_Sessions_TrackId",
                table: "Session",
                newName: "IX_Session_TrackId"
            );

            migrationBuilder.RenameIndex(
                name: "IX_Laps_TrackId",
                table: "Lap",
                newName: "IX_Lap_TrackId"
            );

            migrationBuilder.RenameIndex(
                name: "IX_Laps_SessionId",
                table: "Lap",
                newName: "IX_Lap_SessionId"
            );

            migrationBuilder.RenameIndex(
                name: "IX_Laps_DriverId",
                table: "Lap",
                newName: "IX_Lap_DriverId"
            );

            migrationBuilder.RenameIndex(
                name: "IX_Laps_CarId",
                table: "Lap",
                newName: "IX_Lap_CarId"
            );

            migrationBuilder.AddPrimaryKey(name: "PK_Session", table: "Session", column: "Id");

            migrationBuilder.AddPrimaryKey(name: "PK_Lap", table: "Lap", column: "Id");

            migrationBuilder.AddPrimaryKey(name: "PK_Driver", table: "Driver", column: "Id");

            migrationBuilder.AddPrimaryKey(name: "PK_Car", table: "Car", column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DriverSession_Driver_DriversId",
                table: "DriverSession",
                column: "DriversId",
                principalTable: "Driver",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_DriverSession_Session_SessionsId",
                table: "DriverSession",
                column: "SessionsId",
                principalTable: "Session",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Lap_Car_CarId",
                table: "Lap",
                column: "CarId",
                principalTable: "Car",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Lap_Driver_DriverId",
                table: "Lap",
                column: "DriverId",
                principalTable: "Driver",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Lap_Session_SessionId",
                table: "Lap",
                column: "SessionId",
                principalTable: "Session",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Lap_Tracks_TrackId",
                table: "Lap",
                column: "TrackId",
                principalTable: "Tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Session_Tracks_TrackId",
                table: "Session",
                column: "TrackId",
                principalTable: "Tracks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }
    }
}
