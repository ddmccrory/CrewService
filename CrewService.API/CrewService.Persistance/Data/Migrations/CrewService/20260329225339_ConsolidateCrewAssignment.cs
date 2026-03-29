using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class ConsolidateCrewAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrewAttachmentTemplates");

            migrationBuilder.DropTable(
                name: "CrewOffDays");

            migrationBuilder.DropTable(
                name: "ReliefCoverageRules");

            migrationBuilder.CreateTable(
                name: "CrewAssignments",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CrewCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AssignmentGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DaysOfWeekMask = table.Column<int>(type: "INTEGER", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_CrewAssignments", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CrewAssignments_Crews_CrewCtrlNbr",
                        column: x => x.CrewCtrlNbr,
                        principalTable: "Crews",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrewAssignments_DynamicGroups_AssignmentGroupCtrlNbr",
                        column: x => x.AssignmentGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrewAssignments_AssignmentGroupCtrlNbr",
                table: "CrewAssignments",
                column: "AssignmentGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewAssignments_CrewCtrlNbr",
                table: "CrewAssignments",
                column: "CrewCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrewAssignments");

            migrationBuilder.CreateTable(
                name: "CrewAttachmentTemplates",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AssignmentGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CrewCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrewAttachmentTemplates", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CrewAttachmentTemplates_Crews_CrewCtrlNbr",
                        column: x => x.CrewCtrlNbr,
                        principalTable: "Crews",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrewAttachmentTemplates_DynamicGroups_AssignmentGroupCtrlNbr",
                        column: x => x.AssignmentGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrewOffDays",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CrewPositionCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrewOffDays", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CrewOffDays_CrewPositions_CrewPositionCtrlNbr",
                        column: x => x.CrewPositionCtrlNbr,
                        principalTable: "CrewPositions",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReliefCoverageRules",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AssignmentGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DaysOfWeekMask = table.Column<int>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReliefCrewCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReliefCoverageRules", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_ReliefCoverageRules_Crews_ReliefCrewCtrlNbr",
                        column: x => x.ReliefCrewCtrlNbr,
                        principalTable: "Crews",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReliefCoverageRules_DynamicGroups_AssignmentGroupCtrlNbr",
                        column: x => x.AssignmentGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrewAttachmentTemplates_AssignmentGroupCtrlNbr",
                table: "CrewAttachmentTemplates",
                column: "AssignmentGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewAttachmentTemplates_CrewCtrlNbr",
                table: "CrewAttachmentTemplates",
                column: "CrewCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CrewOffDays_CrewPositionCtrlNbr",
                table: "CrewOffDays",
                column: "CrewPositionCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ReliefCoverageRules_AssignmentGroupCtrlNbr",
                table: "ReliefCoverageRules",
                column: "AssignmentGroupCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_ReliefCoverageRules_ReliefCrewCtrlNbr",
                table: "ReliefCoverageRules",
                column: "ReliefCrewCtrlNbr");
        }
    }
}
