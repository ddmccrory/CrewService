using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddSeniorityMoveRailroadForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SeniorityMoves_RailroadCtrlNbr",
                table: "SeniorityMoves",
                column: "RailroadCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_SeniorityMovePolicies_DynamicGroups_RailroadCtrlNbr",
                table: "SeniorityMovePolicies",
                column: "RailroadCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SeniorityMoves_DynamicGroups_RailroadCtrlNbr",
                table: "SeniorityMoves",
                column: "RailroadCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeniorityMovePolicies_DynamicGroups_RailroadCtrlNbr",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropForeignKey(
                name: "FK_SeniorityMoves_DynamicGroups_RailroadCtrlNbr",
                table: "SeniorityMoves");

            migrationBuilder.DropIndex(
                name: "IX_SeniorityMoves_RailroadCtrlNbr",
                table: "SeniorityMoves");
        }
    }
}
