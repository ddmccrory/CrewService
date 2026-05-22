using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddPendingSeniorityStateChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingSeniorityStateChanges",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    SeniorityCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    FromSeniorityStateCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ToSeniorityStateCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EffectiveDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ScheduledByUserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    ScheduledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledByUserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingSeniorityStateChanges", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_PendingSeniorityStateChanges_SeniorityStates_FromSeniorityStateCtrlNbr",
                        column: x => x.FromSeniorityStateCtrlNbr,
                        principalTable: "SeniorityStates",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PendingSeniorityStateChanges_SeniorityStates_ToSeniorityStateCtrlNbr",
                        column: x => x.ToSeniorityStateCtrlNbr,
                        principalTable: "SeniorityStates",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PendingSeniorityStateChanges_Seniority_SeniorityCtrlNbr",
                        column: x => x.SeniorityCtrlNbr,
                        principalTable: "Seniority",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PendingSeniorityStateChange_Status_EffectiveDate",
                table: "PendingSeniorityStateChanges",
                columns: new[] { "Status", "EffectiveDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PendingSeniorityStateChanges_FromSeniorityStateCtrlNbr",
                table: "PendingSeniorityStateChanges",
                column: "FromSeniorityStateCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PendingSeniorityStateChanges_SeniorityCtrlNbr",
                table: "PendingSeniorityStateChanges",
                column: "SeniorityCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PendingSeniorityStateChanges_ToSeniorityStateCtrlNbr",
                table: "PendingSeniorityStateChanges",
                column: "ToSeniorityStateCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "UIX_PendingSeniorityStateChange_Employee_Pending",
                table: "PendingSeniorityStateChanges",
                column: "EmployeeCtrlNbr",
                unique: true,
                filter: "[Status] = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingSeniorityStateChanges");
        }
    }
}
