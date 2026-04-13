using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RosterBoardUnification_CleanRosterAndSeniority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtraBoard",
                table: "Rosters");

            migrationBuilder.DropColumn(
                name: "OvertimeBoard",
                table: "Rosters");

            migrationBuilder.DropColumn(
                name: "Training",
                table: "Rosters");

            migrationBuilder.RenameColumn(
                name: "StateID",
                table: "Seniority",
                newName: "SeniorityStateCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Seniority_SeniorityStateCtrlNbr",
                table: "Seniority",
                column: "SeniorityStateCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_Seniority_SeniorityStates_SeniorityStateCtrlNbr",
                table: "Seniority",
                column: "SeniorityStateCtrlNbr",
                principalTable: "SeniorityStates",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Seniority_SeniorityStates_SeniorityStateCtrlNbr",
                table: "Seniority");

            migrationBuilder.DropIndex(
                name: "IX_Seniority_SeniorityStateCtrlNbr",
                table: "Seniority");

            migrationBuilder.RenameColumn(
                name: "SeniorityStateCtrlNbr",
                table: "Seniority",
                newName: "StateID");

            migrationBuilder.AddColumn<bool>(
                name: "ExtraBoard",
                table: "Rosters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OvertimeBoard",
                table: "Rosters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Training",
                table: "Rosters",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
