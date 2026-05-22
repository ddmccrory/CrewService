using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddSeniorityStateVacancyConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeniorityStateVacancyConfigs",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ParentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    SeniorityStateCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    VacancyAction = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    TargetBoardCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
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
                    table.PrimaryKey("PK_SeniorityStateVacancyConfigs", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_SeniorityStateVacancyConfigs_DynamicGroups_RailroadCtrlNbr",
                        column: x => x.RailroadCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeniorityStateVacancyConfigs_Parents_ParentCtrlNbr",
                        column: x => x.ParentCtrlNbr,
                        principalTable: "Parents",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeniorityStateVacancyConfigs_RosterBoards_TargetBoardCtrlNbr",
                        column: x => x.TargetBoardCtrlNbr,
                        principalTable: "RosterBoards",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeniorityStateVacancyConfigs_SeniorityStates_SeniorityStateCtrlNbr",
                        column: x => x.SeniorityStateCtrlNbr,
                        principalTable: "SeniorityStates",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityStateVacancyConfigs_ParentCtrlNbr",
                table: "SeniorityStateVacancyConfigs",
                column: "ParentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityStateVacancyConfigs_RailroadCtrlNbr_SeniorityStateCtrlNbr",
                table: "SeniorityStateVacancyConfigs",
                columns: new[] { "RailroadCtrlNbr", "SeniorityStateCtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityStateVacancyConfigs_SeniorityStateCtrlNbr",
                table: "SeniorityStateVacancyConfigs",
                column: "SeniorityStateCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityStateVacancyConfigs_TargetBoardCtrlNbr",
                table: "SeniorityStateVacancyConfigs",
                column: "TargetBoardCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeniorityStateVacancyConfigs");
        }
    }
}
