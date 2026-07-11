using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class SimplifyCallSheetRulesAndRemoveFutureGuard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CallSheetRule",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DepartmentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CallLeadMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    CallDurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    AnchorType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    PostAnchorOffsetMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    SpecialPatterns = table.Column<string>(type: "TEXT", nullable: false),
                    HolidayAdjustment = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    HolidayCustomOffsetMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    GlobalPreCreateOffsetMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_CallSheetRule", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CallSheetRule_Department_DepartmentCtrlNbr",
                        column: x => x.DepartmentCtrlNbr,
                        principalTable: "Department",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CallSheetRule_DepartmentCtrlNbr",
                table: "CallSheetRule",
                column: "DepartmentCtrlNbr",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CallSheetRule");
        }
    }
}
