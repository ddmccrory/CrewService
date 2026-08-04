using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AlignWorkflowEntityTemplateRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowTemplates_RailroadCtrlNbr_TriggerType",
                table: "WorkflowTemplates");

            migrationBuilder.DropColumn(
                name: "TriggerType",
                table: "WorkflowTemplates");

            migrationBuilder.DropColumn(
                name: "TriggerType",
                table: "WorkflowExecutionHistories");

            migrationBuilder.AddColumn<long>(
                name: "TriggerTypeCtrlNbr",
                table: "WorkflowTemplates",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TriggerTypeCtrlNbr",
                table: "WorkflowExecutionHistories",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkflowEffectTypes",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_WorkflowEffectTypes", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowMetadataFieldTypes",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_WorkflowMetadataFieldTypes", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowOperatorTypes",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_WorkflowOperatorTypes", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTriggerTypes",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_WorkflowTriggerTypes", x => x.CtrlNbr);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTemplates_RailroadCtrlNbr_TriggerTypeCtrlNbr",
                table: "WorkflowTemplates",
                columns: new[] { "RailroadCtrlNbr", "TriggerTypeCtrlNbr" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTemplates_TriggerTypeCtrlNbr",
                table: "WorkflowTemplates",
                column: "TriggerTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionHistories_TriggerTypeCtrlNbr",
                table: "WorkflowExecutionHistories",
                column: "TriggerTypeCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowEffectTypes_Code",
                table: "WorkflowEffectTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowMetadataFieldTypes_Code",
                table: "WorkflowMetadataFieldTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowOperatorTypes_Code",
                table: "WorkflowOperatorTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTriggerTypes_Code",
                table: "WorkflowTriggerTypes",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowExecutionHistories_WorkflowTriggerTypes_TriggerTypeCtrlNbr",
                table: "WorkflowExecutionHistories",
                column: "TriggerTypeCtrlNbr",
                principalTable: "WorkflowTriggerTypes",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowTemplates_WorkflowTriggerTypes_TriggerTypeCtrlNbr",
                table: "WorkflowTemplates",
                column: "TriggerTypeCtrlNbr",
                principalTable: "WorkflowTriggerTypes",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowExecutionHistories_WorkflowTriggerTypes_TriggerTypeCtrlNbr",
                table: "WorkflowExecutionHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowTemplates_WorkflowTriggerTypes_TriggerTypeCtrlNbr",
                table: "WorkflowTemplates");

            migrationBuilder.DropTable(
                name: "WorkflowEffectTypes");

            migrationBuilder.DropTable(
                name: "WorkflowMetadataFieldTypes");

            migrationBuilder.DropTable(
                name: "WorkflowOperatorTypes");

            migrationBuilder.DropTable(
                name: "WorkflowTriggerTypes");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowTemplates_RailroadCtrlNbr_TriggerTypeCtrlNbr",
                table: "WorkflowTemplates");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowTemplates_TriggerTypeCtrlNbr",
                table: "WorkflowTemplates");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowExecutionHistories_TriggerTypeCtrlNbr",
                table: "WorkflowExecutionHistories");

            migrationBuilder.DropColumn(
                name: "TriggerTypeCtrlNbr",
                table: "WorkflowTemplates");

            migrationBuilder.DropColumn(
                name: "TriggerTypeCtrlNbr",
                table: "WorkflowExecutionHistories");

            migrationBuilder.AddColumn<string>(
                name: "TriggerType",
                table: "WorkflowTemplates",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TriggerType",
                table: "WorkflowExecutionHistories",
                type: "TEXT",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTemplates_RailroadCtrlNbr_TriggerType",
                table: "WorkflowTemplates",
                columns: new[] { "RailroadCtrlNbr", "TriggerType" });
        }
    }
}
