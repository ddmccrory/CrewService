using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class FixDispatchPositionSlotInstanceFks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DispatchDecisionLogs_PositionSlots_PositionSlotCtrlNbr",
                table: "DispatchDecisionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOverrides_PositionSlots_PositionSlotCtrlNbr",
                table: "DispatchOverrides");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchProjections_PositionSlots_PositionSlotCtrlNbr",
                table: "DispatchProjections");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeBookings_PositionSlots_PositionSlotCtrlNbr",
                table: "EmployeeBookings");

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchDecisionLogs_PositionSlotInstances_PositionSlotCtrlNbr",
                table: "DispatchDecisionLogs",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlotInstances",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOverrides_PositionSlotInstances_PositionSlotCtrlNbr",
                table: "DispatchOverrides",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlotInstances",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchProjections_PositionSlotInstances_PositionSlotCtrlNbr",
                table: "DispatchProjections",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlotInstances",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeBookings_PositionSlotInstances_PositionSlotCtrlNbr",
                table: "EmployeeBookings",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlotInstances",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DispatchDecisionLogs_PositionSlotInstances_PositionSlotCtrlNbr",
                table: "DispatchDecisionLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOverrides_PositionSlotInstances_PositionSlotCtrlNbr",
                table: "DispatchOverrides");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchProjections_PositionSlotInstances_PositionSlotCtrlNbr",
                table: "DispatchProjections");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeBookings_PositionSlotInstances_PositionSlotCtrlNbr",
                table: "EmployeeBookings");

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchDecisionLogs_PositionSlots_PositionSlotCtrlNbr",
                table: "DispatchDecisionLogs",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOverrides_PositionSlots_PositionSlotCtrlNbr",
                table: "DispatchOverrides",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchProjections_PositionSlots_PositionSlotCtrlNbr",
                table: "DispatchProjections",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeBookings_PositionSlots_PositionSlotCtrlNbr",
                table: "EmployeeBookings",
                column: "PositionSlotCtrlNbr",
                principalTable: "PositionSlots",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
