using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddPositionChangeRecordProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PositionChangeRecords",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    EmployeeNotificationCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    SourceType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SourceCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    ChangeType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    EffectiveAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RequiresAcknowledgement = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOpen = table.Column<bool>(type: "INTEGER", nullable: false),
                    OpenedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedReason = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_PositionChangeRecords", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_PositionChangeRecords_EmployeeNotifications_EmployeeNotificationCtrlNbr",
                        column: x => x.EmployeeNotificationCtrlNbr,
                        principalTable: "EmployeeNotifications",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PositionChangeRecords_EmployeeCtrlNbr_IsOpen",
                table: "PositionChangeRecords",
                columns: new[] { "EmployeeCtrlNbr", "IsOpen" });

            migrationBuilder.CreateIndex(
                name: "IX_PositionChangeRecords_EmployeeNotificationCtrlNbr",
                table: "PositionChangeRecords",
                column: "EmployeeNotificationCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_PositionChangeRecords_RailroadCtrlNbr_IsOpen",
                table: "PositionChangeRecords",
                columns: new[] { "RailroadCtrlNbr", "IsOpen" });

            migrationBuilder.CreateIndex(
                name: "IX_PositionChangeRecords_SourceType_SourceCtrlNbr_IsOpen",
                table: "PositionChangeRecords",
                columns: new[] { "SourceType", "SourceCtrlNbr", "IsOpen" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PositionChangeRecords");
        }
    }
}
