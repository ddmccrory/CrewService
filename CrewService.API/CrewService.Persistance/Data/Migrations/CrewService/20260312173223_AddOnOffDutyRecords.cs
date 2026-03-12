using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddOnOffDutyRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OffDutyRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OnDutyRecordCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OffDutyTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalTimeOnDutyMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    RestHoursRequired = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    RestedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConsecutiveDayRestedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReleaseReason = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_OffDutyRecords", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "OnDutyRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    PositionSlotCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    BookingCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    OnDutyTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ScheduledOnDutyTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsLateCall = table.Column<bool>(type: "INTEGER", nullable: false),
                    LateCallAdjustedTimeUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PreviousRestHours = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    ConsecutiveDays = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsAssigned = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_OnDutyRecords", x => x.CtrlNbr);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OffDutyRecords");

            migrationBuilder.DropTable(
                name: "OnDutyRecords");
        }
    }
}
