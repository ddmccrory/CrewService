using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RosterBoardUnification_BoardSlotInstance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardSlotInstances",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ShiftInstanceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RosterBoardCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RosterBoardPositionCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    BoardOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CallSequence = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BoardName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EmployeeName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, defaultValue: ""),
                    PositionName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: ""),
                    DaysWorked = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsecutiveDays = table.Column<int>(type: "INTEGER", nullable: false),
                    RestAvailableAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardSlotInstances", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_BoardSlotInstances_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardSlotInstances_RosterBoardPositions_RosterBoardPositionCtrlNbr",
                        column: x => x.RosterBoardPositionCtrlNbr,
                        principalTable: "RosterBoardPositions",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardSlotInstances_RosterBoards_RosterBoardCtrlNbr",
                        column: x => x.RosterBoardCtrlNbr,
                        principalTable: "RosterBoards",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardSlotInstances_ShiftInstances_ShiftInstanceCtrlNbr",
                        column: x => x.ShiftInstanceCtrlNbr,
                        principalTable: "ShiftInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardSlotInstances_EmployeeCtrlNbr",
                table: "BoardSlotInstances",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSlotInstances_RosterBoardCtrlNbr",
                table: "BoardSlotInstances",
                column: "RosterBoardCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSlotInstances_RosterBoardPositionCtrlNbr",
                table: "BoardSlotInstances",
                column: "RosterBoardPositionCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSlotInstances_ShiftInstanceCtrlNbr",
                table: "BoardSlotInstances",
                column: "ShiftInstanceCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardSlotInstances");
        }
    }
}
