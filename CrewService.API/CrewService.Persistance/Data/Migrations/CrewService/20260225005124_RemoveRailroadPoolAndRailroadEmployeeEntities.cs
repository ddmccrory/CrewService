using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RemoveRailroadPoolAndRailroadEmployeeEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RailroadEmployees");

            migrationBuilder.DropTable(
                name: "RailroadPoolEmployees");

            migrationBuilder.DropTable(
                name: "RailroadPoolPayrollTiers");

            migrationBuilder.DropTable(
                name: "RailroadPools");

            migrationBuilder.RenameColumn(
                name: "RailroadPoolEmployeeCtrlNbr",
                table: "Seniority",
                newName: "EmployeeCtrlNbr");

            migrationBuilder.RenameColumn(
                name: "RailroadPoolCtrlNbr",
                table: "Crafts",
                newName: "DynamicGroupCtrlNbr");

            migrationBuilder.CreateTable(
                name: "PayrollTiers",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DynamicGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    NumberOfDays = table.Column<int>(type: "INTEGER", nullable: false),
                    TypeOfDay = table.Column<int>(type: "INTEGER", nullable: false),
                    RatePercentage = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_PayrollTiers", x => x.CtrlNbr);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollTiers");

            migrationBuilder.RenameColumn(
                name: "EmployeeCtrlNbr",
                table: "Seniority",
                newName: "RailroadPoolEmployeeCtrlNbr");

            migrationBuilder.RenameColumn(
                name: "DynamicGroupCtrlNbr",
                table: "Crafts",
                newName: "RailroadPoolCtrlNbr");

            migrationBuilder.CreateTable(
                name: "RailroadEmployees",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    AssignedPoolsOnly = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RailroadEmployees", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "RailroadPools",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    PoolName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PoolNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RailroadPools", x => x.CtrlNbr);
                });

            migrationBuilder.CreateTable(
                name: "RailroadPoolEmployees",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    RailroadPoolCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RailroadPoolEmployees", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_RailroadPoolEmployees_RailroadPools_RailroadPoolCtrlNbr",
                        column: x => x.RailroadPoolCtrlNbr,
                        principalTable: "RailroadPools",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RailroadPoolPayrollTiers",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    NumberOfDays = table.Column<int>(type: "INTEGER", nullable: false),
                    RailroadPoolCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RatePercentage = table.Column<int>(type: "INTEGER", nullable: false),
                    TypeOfDay = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RailroadPoolPayrollTiers", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_RailroadPoolPayrollTiers_RailroadPools_RailroadPoolCtrlNbr",
                        column: x => x.RailroadPoolCtrlNbr,
                        principalTable: "RailroadPools",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RailroadPoolEmployees_RailroadPoolCtrlNbr",
                table: "RailroadPoolEmployees",
                column: "RailroadPoolCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RailroadPoolPayrollTiers_RailroadPoolCtrlNbr",
                table: "RailroadPoolPayrollTiers",
                column: "RailroadPoolCtrlNbr");
        }
    }
}
