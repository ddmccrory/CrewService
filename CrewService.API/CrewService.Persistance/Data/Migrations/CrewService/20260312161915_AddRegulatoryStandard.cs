using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddRegulatoryStandard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegulatoryStandards",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MaxOnDutyMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    MinRestMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Min8hRestInPreceding24h = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConsecutiveDayLimit6 = table.Column<int>(type: "INTEGER", nullable: false),
                    ConsecutiveDayLimit7 = table.Column<int>(type: "INTEGER", nullable: false),
                    RestAfter6DaysMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    RestAfter7DaysMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    MonthlyCapMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    DeadheadAfter12hMonthlyCapMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    WreckReliefExtraMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_RegulatoryStandards", x => x.CtrlNbr);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegulatoryStandards_Code",
                table: "RegulatoryStandards",
                column: "Code",
                unique: true);

            // Seed CFR Part 228 regulatory standards (system-level, federal law)
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            migrationBuilder.InsertData(
                table: "RegulatoryStandards",
                columns: new[] { "CtrlNbr", "Code", "Description", "MaxOnDutyMinutes", "MinRestMinutes", "Min8hRestInPreceding24h", "ConsecutiveDayLimit6", "ConsecutiveDayLimit7", "RestAfter6DaysMinutes", "RestAfter7DaysMinutes", "MonthlyCapMinutes", "DeadheadAfter12hMonthlyCapMinutes", "WreckReliefExtraMinutes", "EffectiveDate", "CreatedBy_AuditName", "CreatedBy_AuditDateTime", "IsDeleted" },
                values: new object[] { 1L, "CFR-228-TRAIN", "49 CFR Part 228 - Train Employees", 720, 600, true, 6, 7, 2880, 4320, 16560, 1800, 240, new DateOnly(2009, 7, 16), "SYSTEM", now, false });

            migrationBuilder.InsertData(
                table: "RegulatoryStandards",
                columns: new[] { "CtrlNbr", "Code", "Description", "MaxOnDutyMinutes", "MinRestMinutes", "Min8hRestInPreceding24h", "ConsecutiveDayLimit6", "ConsecutiveDayLimit7", "RestAfter6DaysMinutes", "RestAfter7DaysMinutes", "MonthlyCapMinutes", "DeadheadAfter12hMonthlyCapMinutes", "WreckReliefExtraMinutes", "EffectiveDate", "CreatedBy_AuditName", "CreatedBy_AuditDateTime", "IsDeleted" },
                values: new object[] { 2L, "CFR-228-SIGNAL", "49 CFR Part 228 - Signal Employees", 720, 600, false, 6, 7, 2880, 4320, 16560, 1800, 240, new DateOnly(2009, 7, 16), "SYSTEM", now, false });

            migrationBuilder.InsertData(
                table: "RegulatoryStandards",
                columns: new[] { "CtrlNbr", "Code", "Description", "MaxOnDutyMinutes", "MinRestMinutes", "Min8hRestInPreceding24h", "ConsecutiveDayLimit6", "ConsecutiveDayLimit7", "RestAfter6DaysMinutes", "RestAfter7DaysMinutes", "MonthlyCapMinutes", "DeadheadAfter12hMonthlyCapMinutes", "WreckReliefExtraMinutes", "EffectiveDate", "CreatedBy_AuditName", "CreatedBy_AuditDateTime", "IsDeleted" },
                values: new object[] { 3L, "CFR-228-DISPATCH", "49 CFR Part 228 - Dispatching Employees", 720, 600, false, 6, 7, 2880, 4320, 16560, 1800, 240, new DateOnly(2009, 7, 16), "SYSTEM", now, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegulatoryStandards");
        }
    }
}
