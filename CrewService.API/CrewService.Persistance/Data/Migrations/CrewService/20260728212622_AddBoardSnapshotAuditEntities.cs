using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddBoardSnapshotAuditEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TieUpAtUtc",
                table: "BoardSlotInstances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BoardSnapshots",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ShiftInstanceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionSlotInstanceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    VacancyImpactCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    CapturedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TriggerSource = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DecisionSequence = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_BoardSnapshots", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_BoardSnapshots_PositionSlotInstances_PositionSlotInstanceCtrlNbr",
                        column: x => x.PositionSlotInstanceCtrlNbr,
                        principalTable: "PositionSlotInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardSnapshots_ShiftInstances_ShiftInstanceCtrlNbr",
                        column: x => x.ShiftInstanceCtrlNbr,
                        principalTable: "ShiftInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardSnapshots_VacancyImpacts_VacancyImpactCtrlNbr",
                        column: x => x.VacancyImpactCtrlNbr,
                        principalTable: "VacancyImpacts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BoardSelectionDecisions",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ShiftInstanceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionSlotInstanceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    VacancyImpactCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    SnapshotCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    SelectedBoardSlotInstanceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    SelectedEmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DecisionSequence = table.Column<int>(type: "INTEGER", nullable: false),
                    DecisionSource = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DecisionPhase = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DecisionJson = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
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
                    table.PrimaryKey("PK_BoardSelectionDecisions", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_BoardSelectionDecisions_BoardSlotInstances_SelectedBoardSlotInstanceCtrlNbr",
                        column: x => x.SelectedBoardSlotInstanceCtrlNbr,
                        principalTable: "BoardSlotInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardSelectionDecisions_BoardSnapshots_SnapshotCtrlNbr",
                        column: x => x.SnapshotCtrlNbr,
                        principalTable: "BoardSnapshots",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardSelectionDecisions_Employees_SelectedEmployeeCtrlNbr",
                        column: x => x.SelectedEmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardSelectionDecisions_PositionSlotInstances_PositionSlotInstanceCtrlNbr",
                        column: x => x.PositionSlotInstanceCtrlNbr,
                        principalTable: "PositionSlotInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardSelectionDecisions_ShiftInstances_ShiftInstanceCtrlNbr",
                        column: x => x.ShiftInstanceCtrlNbr,
                        principalTable: "ShiftInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardSelectionDecisions_VacancyImpacts_VacancyImpactCtrlNbr",
                        column: x => x.VacancyImpactCtrlNbr,
                        principalTable: "VacancyImpacts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BoardSnapshotRows",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    BoardSnapshotCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    BoardSlotInstanceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ShiftInstanceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RosterBoardCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RosterBoardPositionCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    BoardOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CallSequence = table.Column<long>(type: "INTEGER", nullable: false),
                    TieUpAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    BoardName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EmployeeName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, defaultValue: ""),
                    PositionName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: ""),
                    BoardSnapshotCtrlNbr1 = table.Column<long>(type: "INTEGER", nullable: true),
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
                    table.PrimaryKey("PK_BoardSnapshotRows", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_BoardSnapshotRows_BoardSlotInstances_BoardSlotInstanceCtrlNbr",
                        column: x => x.BoardSlotInstanceCtrlNbr,
                        principalTable: "BoardSlotInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardSnapshotRows_BoardSnapshots_BoardSnapshotCtrlNbr",
                        column: x => x.BoardSnapshotCtrlNbr,
                        principalTable: "BoardSnapshots",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BoardSnapshotRows_BoardSnapshots_BoardSnapshotCtrlNbr1",
                        column: x => x.BoardSnapshotCtrlNbr1,
                        principalTable: "BoardSnapshots",
                        principalColumn: "CtrlNbr");
                    table.ForeignKey(
                        name: "FK_BoardSnapshotRows_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardSnapshotRows_RosterBoardPositions_RosterBoardPositionCtrlNbr",
                        column: x => x.RosterBoardPositionCtrlNbr,
                        principalTable: "RosterBoardPositions",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardSnapshotRows_RosterBoards_RosterBoardCtrlNbr",
                        column: x => x.RosterBoardCtrlNbr,
                        principalTable: "RosterBoards",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BoardSnapshotRows_ShiftInstances_ShiftInstanceCtrlNbr",
                        column: x => x.ShiftInstanceCtrlNbr,
                        principalTable: "ShiftInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardSelectionDecisions_PositionSlotInstanceCtrlNbr",
                table: "BoardSelectionDecisions",
                column: "PositionSlotInstanceCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSelectionDecisions_SelectedBoardSlotInstanceCtrlNbr",
                table: "BoardSelectionDecisions",
                column: "SelectedBoardSlotInstanceCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSelectionDecisions_SelectedEmployeeCtrlNbr",
                table: "BoardSelectionDecisions",
                column: "SelectedEmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSelectionDecisions_ShiftInstanceCtrlNbr_DecisionSequence",
                table: "BoardSelectionDecisions",
                columns: new[] { "ShiftInstanceCtrlNbr", "DecisionSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardSelectionDecisions_SnapshotCtrlNbr",
                table: "BoardSelectionDecisions",
                column: "SnapshotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSelectionDecisions_VacancyImpactCtrlNbr",
                table: "BoardSelectionDecisions",
                column: "VacancyImpactCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSnapshotRows_BoardSlotInstanceCtrlNbr",
                table: "BoardSnapshotRows",
                column: "BoardSlotInstanceCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSnapshotRows_BoardSnapshotCtrlNbr_BoardOrder_CallSequence_CtrlNbr",
                table: "BoardSnapshotRows",
                columns: new[] { "BoardSnapshotCtrlNbr", "BoardOrder", "CallSequence", "CtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardSnapshotRows_BoardSnapshotCtrlNbr1",
                table: "BoardSnapshotRows",
                column: "BoardSnapshotCtrlNbr1");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSnapshotRows_EmployeeCtrlNbr",
                table: "BoardSnapshotRows",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSnapshotRows_RosterBoardCtrlNbr",
                table: "BoardSnapshotRows",
                column: "RosterBoardCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSnapshotRows_RosterBoardPositionCtrlNbr",
                table: "BoardSnapshotRows",
                column: "RosterBoardPositionCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSnapshotRows_ShiftInstanceCtrlNbr",
                table: "BoardSnapshotRows",
                column: "ShiftInstanceCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSnapshots_PositionSlotInstanceCtrlNbr",
                table: "BoardSnapshots",
                column: "PositionSlotInstanceCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_BoardSnapshots_ShiftInstanceCtrlNbr_DecisionSequence",
                table: "BoardSnapshots",
                columns: new[] { "ShiftInstanceCtrlNbr", "DecisionSequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BoardSnapshots_VacancyImpactCtrlNbr",
                table: "BoardSnapshots",
                column: "VacancyImpactCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardSelectionDecisions");

            migrationBuilder.DropTable(
                name: "BoardSnapshotRows");

            migrationBuilder.DropTable(
                name: "BoardSnapshots");

            migrationBuilder.DropColumn(
                name: "TieUpAtUtc",
                table: "BoardSlotInstances");
        }
    }
}
