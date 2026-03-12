using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddAbsenceCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AbsenceCodeCraftOverrides",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AbsenceCodeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    OverrideAutoMarkUpHours = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_AbsenceCodeCraftOverrides", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceCodes",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsExcused = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCompensated = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystemOnly = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsHolidayExempt = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultAutoMarkUpHours = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_AbsenceCodes", x => x.CtrlNbr);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceCodeCraftOverrides_AbsenceCodeCtrlNbr_CraftCtrlNbr",
                table: "AbsenceCodeCraftOverrides",
                columns: new[] { "AbsenceCodeCtrlNbr", "CraftCtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceCodes_Code",
                table: "AbsenceCodes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbsenceCodeCraftOverrides");

            migrationBuilder.DropTable(
                name: "AbsenceCodes");
        }
    }
}
