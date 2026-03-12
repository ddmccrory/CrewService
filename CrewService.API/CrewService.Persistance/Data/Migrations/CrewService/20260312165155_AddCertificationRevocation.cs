using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddCertificationRevocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CertificationRevocationRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCertificationCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ViolationType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ViolationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SuspendedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WrittenNoticeAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HearingScheduledUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HearingHeldUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PresidingOfficerCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    Decision = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    DecisionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevocationPeriodMonths = table.Column<int>(type: "INTEGER", nullable: true),
                    RevocationEndsUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HearingRecordRetainUntil = table.Column<DateOnly>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_CertificationRevocationRecords", x => x.CtrlNbr);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificationRevocationRecords");
        }
    }
}
