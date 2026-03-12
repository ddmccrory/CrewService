using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddEmployeeCertification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeCertifications",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RegulatoryQualificationCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CertificationType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CertificationDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ExpirationDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CertificationNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SuspendedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SuspensionReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RevocationPeriodEndUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastMonitoringObservationUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastComplianceTestUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeCertifications", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "CertificationEligibilityChecks",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCertificationCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CheckType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EvaluationDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StalenessLimitDays = table.Column<int>(type: "INTEGER", nullable: false),
                    ExpiresAtDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Result = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EvaluatorName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_CertificationEligibilityChecks", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CertificationEligibilityChecks_EmployeeCertifications_EmployeeCertificationCtrlNbr",
                        column: x => x.EmployeeCertificationCtrlNbr,
                        principalTable: "EmployeeCertifications",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificationEligibilityChecks_EmployeeCertificationCtrlNbr",
                table: "CertificationEligibilityChecks",
                column: "EmployeeCertificationCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificationEligibilityChecks");

            migrationBuilder.DropTable(
                name: "EmployeeCertifications");
        }
    }
}
