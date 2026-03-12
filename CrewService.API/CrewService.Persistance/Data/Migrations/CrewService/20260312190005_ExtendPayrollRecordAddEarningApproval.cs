using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class ExtendPayrollRecordAddEarningApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OnDutyRecordCtrlNbr",
                table: "PayrollRecord",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresApproval",
                table: "PayrollRecord",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedEarningCode",
                table: "PayrollRecord",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EarningApprovals",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PayrollRecordCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ApprovalTier = table.Column<int>(type: "INTEGER", nullable: false),
                    OfficerCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
                    table.PrimaryKey("PK_EarningApprovals", x => x.CtrlNbr);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EarningApprovals");

            migrationBuilder.DropColumn(
                name: "OnDutyRecordCtrlNbr",
                table: "PayrollRecord");

            migrationBuilder.DropColumn(
                name: "RequiresApproval",
                table: "PayrollRecord");

            migrationBuilder.DropColumn(
                name: "ResolvedEarningCode",
                table: "PayrollRecord");
        }
    }
}
