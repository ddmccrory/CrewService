using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddWorkflowExecutionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowExecutionHistories",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkflowTemplateCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkflowVersionCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkflowVersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    TriggerType = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    AggregateCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    CorrelationId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    DetailsJson = table.Column<string>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_WorkflowExecutionHistories", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_WorkflowExecutionHistories_WorkflowTemplates_WorkflowTemplateCtrlNbr",
                        column: x => x.WorkflowTemplateCtrlNbr,
                        principalTable: "WorkflowTemplates",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowExecutionHistories_WorkflowVersions_WorkflowVersionCtrlNbr",
                        column: x => x.WorkflowVersionCtrlNbr,
                        principalTable: "WorkflowVersions",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionHistories_RailroadCtrlNbr",
                table: "WorkflowExecutionHistories",
                column: "RailroadCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionHistories_WorkflowTemplateCtrlNbr_StartedAtUtc",
                table: "WorkflowExecutionHistories",
                columns: new[] { "WorkflowTemplateCtrlNbr", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowExecutionHistories_WorkflowVersionCtrlNbr",
                table: "WorkflowExecutionHistories",
                column: "WorkflowVersionCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowExecutionHistories");
        }
    }
}
