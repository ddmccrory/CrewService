using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddRailroadCtrlNbrToSeniorityMovePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SeniorityMovePolicies_CraftCtrlNbr",
                table: "SeniorityMovePolicies");

            migrationBuilder.AddColumn<long>(
                name: "RailroadCtrlNbr",
                table: "SeniorityMoves",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "RailroadCtrlNbr",
                table: "SeniorityMovePolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMovePolicies_CraftCtrlNbr",
                table: "SeniorityMovePolicies",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMovePolicies_RailroadCtrlNbr_CraftCtrlNbr",
                table: "SeniorityMovePolicies",
                columns: new[] { "RailroadCtrlNbr", "CraftCtrlNbr" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SeniorityMovePolicies_CraftCtrlNbr",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropIndex(
                name: "IX_SeniorityMovePolicies_RailroadCtrlNbr_CraftCtrlNbr",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropColumn(
                name: "RailroadCtrlNbr",
                table: "SeniorityMoves");

            migrationBuilder.DropColumn(
                name: "RailroadCtrlNbr",
                table: "SeniorityMovePolicies");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMovePolicies_CraftCtrlNbr",
                table: "SeniorityMovePolicies",
                column: "CraftCtrlNbr",
                unique: true);
        }
    }
}
