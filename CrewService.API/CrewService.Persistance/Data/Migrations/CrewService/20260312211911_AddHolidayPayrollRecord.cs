using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddHolidayPayrollRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HolidayPayrollRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    HolidayCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PayrollRecordCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    IsQualified = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisqualificationReason = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_HolidayPayrollRecords", x => x.CtrlNbr);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HolidayPayrollRecords");
        }
    }
}
