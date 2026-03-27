using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Devlabs.AcTiming.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCarDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CarDefinitions",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Brand = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DisplayName = table.Column<string>(
                        type: "TEXT",
                        maxLength: 200,
                        nullable: true
                    ),
                    LogoSlug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsConfigured = table.Column<bool>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarDefinitions", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_CarDefinitions_Model",
                table: "CarDefinitions",
                column: "Model",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CarDefinitions");

            migrationBuilder.RenameColumn(name: "CarDefinitionId", table: "Lap", newName: "CarId");
        }
    }
}
