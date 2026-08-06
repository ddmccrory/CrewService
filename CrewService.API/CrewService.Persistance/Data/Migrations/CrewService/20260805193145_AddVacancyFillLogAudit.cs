using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddVacancyFillLogAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VacancyFillLogs",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ShiftInstanceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionSlotCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OnDutyRecordCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    AssignmentCode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CraftRoleName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ForceOverride = table.Column<bool>(type: "INTEGER", nullable: false),
                    ForceReason = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Accepted = table.Column<bool>(type: "INTEGER", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsLateCall = table.Column<bool>(type: "INTEGER", nullable: false),
                    LateCallNote = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    ArrivalFollowUpNote = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    DispatcherNote = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_VacancyFillLogs", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_VacancyFillLogs_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacancyFillLogs_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacancyFillLogs_OnDutyRecords_OnDutyRecordCtrlNbr",
                        column: x => x.OnDutyRecordCtrlNbr,
                        principalTable: "OnDutyRecords",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacancyFillLogs_PositionSlotInstances_PositionSlotCtrlNbr",
                        column: x => x.PositionSlotCtrlNbr,
                        principalTable: "PositionSlotInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacancyFillLogs_ShiftInstances_ShiftInstanceCtrlNbr",
                        column: x => x.ShiftInstanceCtrlNbr,
                        principalTable: "ShiftInstances",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VacancyFillLogs_EmployeeCtrlNbr",
                table: "VacancyFillLogs",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyFillLogs_OnDutyRecordCtrlNbr",
                table: "VacancyFillLogs",
                column: "OnDutyRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyFillLogs_PositionSlotCtrlNbr",
                table: "VacancyFillLogs",
                column: "PositionSlotCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyFillLogs_ShiftInstanceCtrlNbr",
                table: "VacancyFillLogs",
                column: "ShiftInstanceCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_VacancyFillLogs_WorkAreaGroupCtrlNbr",
                table: "VacancyFillLogs",
                column: "WorkAreaGroupCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VacancyFillLogs");
        }
    }
}
