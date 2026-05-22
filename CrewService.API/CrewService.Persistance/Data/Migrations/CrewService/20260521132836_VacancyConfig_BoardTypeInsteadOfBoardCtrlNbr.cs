using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class VacancyConfig_BoardTypeInsteadOfBoardCtrlNbr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeniorityStateVacancyConfigs_RosterBoards_TargetBoardCtrlNbr",
                table: "SeniorityStateVacancyConfigs");

            migrationBuilder.DropIndex(
                name: "IX_SeniorityStateVacancyConfigs_TargetBoardCtrlNbr",
                table: "SeniorityStateVacancyConfigs");

            migrationBuilder.DropColumn(
                name: "TargetBoardCtrlNbr",
                table: "SeniorityStateVacancyConfigs");

            migrationBuilder.AddColumn<string>(
                name: "TargetBoardType",
                table: "SeniorityStateVacancyConfigs",
                type: "TEXT",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetBoardType",
                table: "SeniorityStateVacancyConfigs");

            migrationBuilder.AddColumn<long>(
                name: "TargetBoardCtrlNbr",
                table: "SeniorityStateVacancyConfigs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityStateVacancyConfigs_TargetBoardCtrlNbr",
                table: "SeniorityStateVacancyConfigs",
                column: "TargetBoardCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_SeniorityStateVacancyConfigs_RosterBoards_TargetBoardCtrlNbr",
                table: "SeniorityStateVacancyConfigs",
                column: "TargetBoardCtrlNbr",
                principalTable: "RosterBoards",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
