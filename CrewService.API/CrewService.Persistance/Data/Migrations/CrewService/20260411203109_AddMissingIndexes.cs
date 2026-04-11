using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddMissingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DynamicGroups_Path",
                table: "DynamicGroups",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicGroups_RailroadCtrlNbr",
                table: "DynamicGroups",
                column: "RailroadCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DynamicGroups_Path",
                table: "DynamicGroups");

            migrationBuilder.DropIndex(
                name: "IX_DynamicGroups_RailroadCtrlNbr",
                table: "DynamicGroups");
        }
    }
}
