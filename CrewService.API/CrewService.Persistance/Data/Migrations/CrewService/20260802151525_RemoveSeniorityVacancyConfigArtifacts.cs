using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RemoveSeniorityVacancyConfigArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeniorityStateTypeVacancyDefaults");

            migrationBuilder.DropTable(
                name: "SeniorityStateVacancyConfigs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeniorityStateTypeVacancyDefaults",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DefaultVacancyAction = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StateType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeniorityStateTypeVacancyDefaults", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_SeniorityStateTypeVacancyDefaults_DynamicGroups_RailroadCtrlNbr",
                        column: x => x.RailroadCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeniorityStateTypeVacancyDefaults_Parents_ParentCtrlNbr",
                        column: x => x.ParentCtrlNbr,
                        principalTable: "Parents",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeniorityStateVacancyConfigs",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    SeniorityStateCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    TargetBoardType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    VacancyAction = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
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
                        name: "FK_SeniorityStateVacancyConfigs_SeniorityStates_SeniorityStateCtrlNbr",
                        column: x => x.SeniorityStateCtrlNbr,
                        principalTable: "SeniorityStates",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityStateTypeVacancyDefaults_ParentCtrlNbr",
                table: "SeniorityStateTypeVacancyDefaults",
                column: "ParentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_SeniorityStateTypeVacancyDefaults_RailroadCtrlNbr_StateType",
                table: "SeniorityStateTypeVacancyDefaults",
                columns: new[] { "RailroadCtrlNbr", "StateType" },
                unique: true);

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
        }
    }
}
