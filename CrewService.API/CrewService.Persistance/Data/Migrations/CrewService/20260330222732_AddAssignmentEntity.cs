using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddAssignmentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrewAssignments_DynamicGroups_AssignmentGroupCtrlNbr",
                table: "CrewAssignments");

            migrationBuilder.RenameColumn(
                name: "AssignmentGroupCtrlNbr",
                table: "CrewAssignments",
                newName: "AssignmentCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_CrewAssignments_AssignmentGroupCtrlNbr",
                table: "CrewAssignments",
                newName: "IX_CrewAssignments_AssignmentCtrlNbr");

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DepartmentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OnDutyTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    IsExtra = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_Assignments", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_Assignments_Department_DepartmentCtrlNbr",
                        column: x => x.DepartmentCtrlNbr,
                        principalTable: "Department",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assignments_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentSchedules",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AssignmentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ShiftDefinitionCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OperatingDaysMask = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_AssignmentSchedules", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_AssignmentSchedules_Assignments_AssignmentCtrlNbr",
                        column: x => x.AssignmentCtrlNbr,
                        principalTable: "Assignments",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentSchedules_ShiftDefinitions_ShiftDefinitionCtrlNbr",
                        column: x => x.ShiftDefinitionCtrlNbr,
                        principalTable: "ShiftDefinitions",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_DepartmentCtrlNbr",
                table: "Assignments",
                column: "DepartmentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_WorkAreaGroupCtrlNbr",
                table: "Assignments",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSchedules_AssignmentCtrlNbr",
                table: "AssignmentSchedules",
                column: "AssignmentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentSchedules_ShiftDefinitionCtrlNbr",
                table: "AssignmentSchedules",
                column: "ShiftDefinitionCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_CrewAssignments_Assignments_AssignmentCtrlNbr",
                table: "CrewAssignments",
                column: "AssignmentCtrlNbr",
                principalTable: "Assignments",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrewAssignments_Assignments_AssignmentCtrlNbr",
                table: "CrewAssignments");

            migrationBuilder.DropTable(
                name: "AssignmentSchedules");

            migrationBuilder.DropTable(
                name: "Assignments");

            migrationBuilder.RenameColumn(
                name: "AssignmentCtrlNbr",
                table: "CrewAssignments",
                newName: "AssignmentGroupCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_CrewAssignments_AssignmentCtrlNbr",
                table: "CrewAssignments",
                newName: "IX_CrewAssignments_AssignmentGroupCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_CrewAssignments_DynamicGroups_AssignmentGroupCtrlNbr",
                table: "CrewAssignments",
                column: "AssignmentGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
