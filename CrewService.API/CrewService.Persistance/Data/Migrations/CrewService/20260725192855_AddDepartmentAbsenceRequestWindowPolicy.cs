using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddDepartmentAbsenceRequestWindowPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepartmentAbsenceRequestWindowPolicy",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DepartmentCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RequestWindowCapDays = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_DepartmentAbsenceRequestWindowPolicy", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_DepartmentAbsenceRequestWindowPolicy_Department_DepartmentCtrlNbr",
                        column: x => x.DepartmentCtrlNbr,
                        principalTable: "Department",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentAbsenceRequestWindowPolicy_DepartmentCtrlNbr",
                table: "DepartmentAbsenceRequestWindowPolicy",
                column: "DepartmentCtrlNbr",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartmentAbsenceRequestWindowPolicy");
        }
    }
}
