using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RenameAssignmentTemplateToAssignmentGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrewAttachmentTemplates_AssignmentTemplates_AssignmentTemplateCtrlNbr",
                table: "CrewAttachmentTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_ReliefCoverageRules_AssignmentTemplates_AssignmentTemplateCtrlNbr",
                table: "ReliefCoverageRules");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkInstances_AssignmentTemplates_AssignmentTemplateCtrlNbr",
                table: "WorkInstances");

            migrationBuilder.DropTable(
                name: "AssignmentTemplates");

            migrationBuilder.RenameColumn(
                name: "AssignmentTemplateCtrlNbr",
                table: "WorkInstances",
                newName: "AssignmentGroupCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_WorkInstances_AssignmentTemplateCtrlNbr",
                table: "WorkInstances",
                newName: "IX_WorkInstances_AssignmentGroupCtrlNbr");

            migrationBuilder.RenameColumn(
                name: "AssignmentTemplateCtrlNbr",
                table: "ReliefCoverageRules",
                newName: "AssignmentGroupCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_ReliefCoverageRules_AssignmentTemplateCtrlNbr",
                table: "ReliefCoverageRules",
                newName: "IX_ReliefCoverageRules_AssignmentGroupCtrlNbr");

            migrationBuilder.RenameColumn(
                name: "AssignmentTemplateCtrlNbr",
                table: "CrewAttachmentTemplates",
                newName: "AssignmentGroupCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_CrewAttachmentTemplates_AssignmentTemplateCtrlNbr",
                table: "CrewAttachmentTemplates",
                newName: "IX_CrewAttachmentTemplates_AssignmentGroupCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_CrewAttachmentTemplates_DynamicGroups_AssignmentGroupCtrlNbr",
                table: "CrewAttachmentTemplates",
                column: "AssignmentGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReliefCoverageRules_DynamicGroups_AssignmentGroupCtrlNbr",
                table: "ReliefCoverageRules",
                column: "AssignmentGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkInstances_DynamicGroups_AssignmentGroupCtrlNbr",
                table: "WorkInstances",
                column: "AssignmentGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrewAttachmentTemplates_DynamicGroups_AssignmentGroupCtrlNbr",
                table: "CrewAttachmentTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_ReliefCoverageRules_DynamicGroups_AssignmentGroupCtrlNbr",
                table: "ReliefCoverageRules");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkInstances_DynamicGroups_AssignmentGroupCtrlNbr",
                table: "WorkInstances");

            migrationBuilder.RenameColumn(
                name: "AssignmentGroupCtrlNbr",
                table: "WorkInstances",
                newName: "AssignmentTemplateCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_WorkInstances_AssignmentGroupCtrlNbr",
                table: "WorkInstances",
                newName: "IX_WorkInstances_AssignmentTemplateCtrlNbr");

            migrationBuilder.RenameColumn(
                name: "AssignmentGroupCtrlNbr",
                table: "ReliefCoverageRules",
                newName: "AssignmentTemplateCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_ReliefCoverageRules_AssignmentGroupCtrlNbr",
                table: "ReliefCoverageRules",
                newName: "IX_ReliefCoverageRules_AssignmentTemplateCtrlNbr");

            migrationBuilder.RenameColumn(
                name: "AssignmentGroupCtrlNbr",
                table: "CrewAttachmentTemplates",
                newName: "AssignmentTemplateCtrlNbr");

            migrationBuilder.RenameIndex(
                name: "IX_CrewAttachmentTemplates_AssignmentGroupCtrlNbr",
                table: "CrewAttachmentTemplates",
                newName: "IX_CrewAttachmentTemplates_AssignmentTemplateCtrlNbr");

            migrationBuilder.CreateTable(
                name: "AssignmentTemplates",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RecurrenceJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentTemplates", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_AssignmentTemplates_DynamicGroups_WorkAreaGroupCtrlNbr",
                        column: x => x.WorkAreaGroupCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentTemplates_WorkAreaGroupCtrlNbr",
                table: "AssignmentTemplates",
                column: "WorkAreaGroupCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_CrewAttachmentTemplates_AssignmentTemplates_AssignmentTemplateCtrlNbr",
                table: "CrewAttachmentTemplates",
                column: "AssignmentTemplateCtrlNbr",
                principalTable: "AssignmentTemplates",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReliefCoverageRules_AssignmentTemplates_AssignmentTemplateCtrlNbr",
                table: "ReliefCoverageRules",
                column: "AssignmentTemplateCtrlNbr",
                principalTable: "AssignmentTemplates",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkInstances_AssignmentTemplates_AssignmentTemplateCtrlNbr",
                table: "WorkInstances",
                column: "AssignmentTemplateCtrlNbr",
                principalTable: "AssignmentTemplates",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
