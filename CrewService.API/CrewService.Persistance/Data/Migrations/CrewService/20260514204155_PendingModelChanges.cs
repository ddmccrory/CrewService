using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class PendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "WorkAreaGroupCtrlNbr",
                table: "PositionVacancies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveUtc",
                table: "Bulletins",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "BulletinRules",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    BidWindowHours = table.Column<int>(type: "INTEGER", nullable: false),
                    BidWindowStartTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    BidWindowCloseTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    EffectiveOffsetDays = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectiveTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    ForceAssignHours = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_BulletinRules", x => x.CtrlNbr);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BulletinRules_CraftCtrlNbr",
                table: "BulletinRules",
                column: "CraftCtrlNbr",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulletinRules");

            migrationBuilder.DropColumn(
                name: "WorkAreaGroupCtrlNbr",
                table: "PositionVacancies");

            migrationBuilder.DropColumn(
                name: "EffectiveUtc",
                table: "Bulletins");
        }
    }
}
