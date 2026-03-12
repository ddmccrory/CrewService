using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddCraftOperationsPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CraftOperationsPolicies",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    LateCallThresholdMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    RestCalculationStrategy = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FixedRestHours = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    ConsecutiveDayResetHours = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    DeleteConflictingNextShift = table.Column<bool>(type: "INTEGER", nullable: false),
                    AutoAnnulCreatesOffDuty = table.Column<bool>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_CraftOperationsPolicies", x => x.CtrlNbr);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CraftOperationsPolicies_CraftCtrlNbr",
                table: "CraftOperationsPolicies",
                column: "CraftCtrlNbr",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CraftOperationsPolicies");
        }
    }
}
