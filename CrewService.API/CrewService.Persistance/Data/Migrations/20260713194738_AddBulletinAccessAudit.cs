using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBulletinAccessAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BulletinAccessAudits",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    BulletinCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ViewedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_BulletinAccessAudits", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_BulletinAccessAudits_Bulletins_BulletinCtrlNbr",
                        column: x => x.BulletinCtrlNbr,
                        principalTable: "Bulletins",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BulletinAccessAudits_Employees_EmployeeCtrlNbr",
                        column: x => x.EmployeeCtrlNbr,
                        principalTable: "Employees",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BulletinAccessAudits_BulletinCtrlNbr_EmployeeCtrlNbr_ViewedAtUtc",
                table: "BulletinAccessAudits",
                columns: new[] { "BulletinCtrlNbr", "EmployeeCtrlNbr", "ViewedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BulletinAccessAudits_EmployeeCtrlNbr",
                table: "BulletinAccessAudits",
                column: "EmployeeCtrlNbr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulletinAccessAudits");
        }
    }
}
