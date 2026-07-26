using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddAbsenceWaitListEntitiesAndPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AbsenceRequestWaitListRecord",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AbsenceCodeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RequestDateUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EntryUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WaitListType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    DepartmentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    AssignedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AssignmentNotes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_AbsenceRequestWaitListRecord", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_AbsenceRequestWaitListRecord_AbsenceCodes_AbsenceCodeCtrlNbr",
                        column: x => x.AbsenceCodeCtrlNbr,
                        principalTable: "AbsenceCodes",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AbsenceRequestWaitListRecord_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AbsenceRequestWaitListRecord_Department_DepartmentCtrlNbr",
                        column: x => x.DepartmentCtrlNbr,
                        principalTable: "Department",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AbsenceRequestWaitListRecord_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceWaitListAllowancePolicy",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WaitListType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AllowanceCode = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CalendarYear = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxAssignments = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_AbsenceWaitListAllowancePolicy", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_AbsenceWaitListAllowancePolicy_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceRequestWaitListLink",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AbsenceRequestCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AbsenceRequestWaitListRecordCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_AbsenceRequestWaitListLink", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_AbsenceRequestWaitListLink_AbsenceRequestWaitListRecord_AbsenceRequestWaitListRecordCtrlNbr",
                        column: x => x.AbsenceRequestWaitListRecordCtrlNbr,
                        principalTable: "AbsenceRequestWaitListRecord",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AbsenceRequestWaitListLink_AbsenceRequests_AbsenceRequestCtrlNbr",
                        column: x => x.AbsenceRequestCtrlNbr,
                        principalTable: "AbsenceRequests",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequestWaitListLink_AbsenceRequestCtrlNbr_AbsenceRequestWaitListRecordCtrlNbr",
                table: "AbsenceRequestWaitListLink",
                columns: new[] { "AbsenceRequestCtrlNbr", "AbsenceRequestWaitListRecordCtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequestWaitListLink_AbsenceRequestWaitListRecordCtrlNbr",
                table: "AbsenceRequestWaitListLink",
                column: "AbsenceRequestWaitListRecordCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequestWaitListRecord_AbsenceCodeCtrlNbr",
                table: "AbsenceRequestWaitListRecord",
                column: "AbsenceCodeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequestWaitListRecord_CraftCtrlNbr",
                table: "AbsenceRequestWaitListRecord",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequestWaitListRecord_DepartmentCtrlNbr",
                table: "AbsenceRequestWaitListRecord",
                column: "DepartmentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequestWaitListRecord_EmployeeCtrlNbr",
                table: "AbsenceRequestWaitListRecord",
                column: "EmployeeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceRequestWaitListRecord_WaitListType_RequestDateUtc_AssignedAtUtc_EntryUtc",
                table: "AbsenceRequestWaitListRecord",
                columns: new[] { "WaitListType", "RequestDateUtc", "AssignedAtUtc", "EntryUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceWaitListAllowancePolicy_CraftCtrlNbr_WaitListType_AllowanceCode_CalendarYear",
                table: "AbsenceWaitListAllowancePolicy",
                columns: new[] { "CraftCtrlNbr", "WaitListType", "AllowanceCode", "CalendarYear" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbsenceRequestWaitListLink");

            migrationBuilder.DropTable(
                name: "AbsenceWaitListAllowancePolicy");

            migrationBuilder.DropTable(
                name: "AbsenceRequestWaitListRecord");
        }
    }
}
