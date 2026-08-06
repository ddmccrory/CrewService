using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RenameOrderTieUpTimeUtcToTieUpOrderUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrderTieUpTimeUtc",
                table: "RosterBoardPositions",
                newName: "TieUpOrderUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TieUpOrderUtc",
                table: "RosterBoardPositions",
                newName: "OrderTieUpTimeUtc");
        }
    }
}
