using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RepointOnDutyRecordPositionSlotFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OnDutyRecords_PositionSlots_PositionSlotCtrlNbr",
                table: "OnDutyRecords");

            migrationBuilder.AddForeignKey(
                name: "FK_OnDutyRecords_PositionSlotInstances_PositionSlotCtrlNbr",
                table: "OnDutyRecords",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlotInstances",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OnDutyRecords_PositionSlotInstances_PositionSlotCtrlNbr",
                table: "OnDutyRecords");

            migrationBuilder.AddForeignKey(
                name: "FK_OnDutyRecords_PositionSlots_PositionSlotCtrlNbr",
                table: "OnDutyRecords",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
