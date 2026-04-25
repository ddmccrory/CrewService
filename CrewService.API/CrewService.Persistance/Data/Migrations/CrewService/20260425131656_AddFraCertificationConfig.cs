using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddFraCertificationConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastComplianceTestUtc",
                table: "EmployeeCertifications");

            migrationBuilder.DropColumn(
                name: "LastMonitoringObservationUtc",
                table: "EmployeeCertifications");

            migrationBuilder.CreateTable(
                name: "FraCertificationCheckConfigs",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ParentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    CheckType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StalenessLimitDays = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnforced = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnforcementLocked = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_FraCertificationCheckConfigs", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_FraCertificationCheckConfigs_DynamicGroups_RailroadCtrlNbr",
                        column: x => x.RailroadCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FraCertificationCheckConfigs_Parents_ParentCtrlNbr",
                        column: x => x.ParentCtrlNbr,
                        principalTable: "Parents",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FraCertificationConfigs",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ParentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    CertCycleMonths = table.Column<int>(type: "INTEGER", nullable: false),
                    RecertWindowDays = table.Column<int>(type: "INTEGER", nullable: false),
                    RenewWindowDays = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_FraCertificationConfigs", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_FraCertificationConfigs_DynamicGroups_RailroadCtrlNbr",
                        column: x => x.RailroadCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FraCertificationConfigs_Parents_ParentCtrlNbr",
                        column: x => x.ParentCtrlNbr,
                        principalTable: "Parents",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FraCertificationCheckConfigs_ParentCtrlNbr",
                table: "FraCertificationCheckConfigs",
                column: "ParentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_FraCertificationCheckConfigs_RailroadCtrlNbr",
                table: "FraCertificationCheckConfigs",
                column: "RailroadCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_FraCertificationConfigs_ParentCtrlNbr",
                table: "FraCertificationConfigs",
                column: "ParentCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_FraCertificationConfigs_RailroadCtrlNbr",
                table: "FraCertificationConfigs",
                column: "RailroadCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FraCertificationCheckConfigs");

            migrationBuilder.DropTable(
                name: "FraCertificationConfigs");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastComplianceTestUtc",
                table: "EmployeeCertifications",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMonitoringObservationUtc",
                table: "EmployeeCertifications",
                type: "TEXT",
                nullable: true);
        }
    }
}
