using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class MoveWorkAreaFromBoardToRoster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RosterBoards_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "RosterBoards");

            migrationBuilder.DropIndex(
                name: "IX_RosterBoards_WorkAreaGroupCtrlNbr",
                table: "RosterBoards");

            migrationBuilder.DropColumn(
                name: "WorkAreaGroupCtrlNbr",
                table: "RosterBoards");

            migrationBuilder.AddColumn<long>(
                name: "WorkAreaGroupCtrlNbr",
                table: "Rosters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Rosters_WorkAreaGroupCtrlNbr",
                table: "Rosters",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_Rosters_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "Rosters",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rosters_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "Rosters");

            migrationBuilder.DropIndex(
                name: "IX_Rosters_WorkAreaGroupCtrlNbr",
                table: "Rosters");

            migrationBuilder.DropColumn(
                name: "WorkAreaGroupCtrlNbr",
                table: "Rosters");

            migrationBuilder.AddColumn<long>(
                name: "WorkAreaGroupCtrlNbr",
                table: "RosterBoards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_RosterBoards_WorkAreaGroupCtrlNbr",
                table: "RosterBoards",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_RosterBoards_DynamicGroups_WorkAreaGroupCtrlNbr",
                table: "RosterBoards",
                column: "WorkAreaGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
