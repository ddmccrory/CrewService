using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RepointVacancyImpactPositionSlotFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VacancyImpacts_PositionSlots_PositionSlotCtrlNbr",
                table: "VacancyImpacts");

            migrationBuilder.AddForeignKey(
                name: "FK_VacancyImpacts_PositionSlotInstances_PositionSlotCtrlNbr",
                table: "VacancyImpacts",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlotInstances",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VacancyImpacts_PositionSlotInstances_PositionSlotCtrlNbr",
                table: "VacancyImpacts");

            migrationBuilder.AddForeignKey(
                name: "FK_VacancyImpacts_PositionSlots_PositionSlotCtrlNbr",
                table: "VacancyImpacts",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
