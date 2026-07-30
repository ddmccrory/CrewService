using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddCraftRoleDefaultRosterBoardLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DefaultRosterBoardCtrlNbr",
                table: "CraftRoles",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CraftRoles_DefaultRosterBoardCtrlNbr",
                table: "CraftRoles",
                column: "DefaultRosterBoardCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_CraftRoles_RosterBoards_DefaultRosterBoardCtrlNbr",
                table: "CraftRoles",
                column: "DefaultRosterBoardCtrlNbr",
                principalTable: "RosterBoards",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CraftRoles_RosterBoards_DefaultRosterBoardCtrlNbr",
                table: "CraftRoles");

            migrationBuilder.DropIndex(
                name: "IX_CraftRoles_DefaultRosterBoardCtrlNbr",
                table: "CraftRoles");

            migrationBuilder.DropColumn(
                name: "DefaultRosterBoardCtrlNbr",
                table: "CraftRoles");
        }
    }
}
