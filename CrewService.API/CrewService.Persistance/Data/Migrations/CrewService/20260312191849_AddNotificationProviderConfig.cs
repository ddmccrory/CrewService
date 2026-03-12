using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddNotificationProviderConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationProviderConfigs",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    WorkAreaGroupCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    ProviderType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ConfigJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    PollingIntervalSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    PollingTimeoutMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    BatchSize = table.Column<int>(type: "INTEGER", nullable: false),
                    BatchPauseSeconds = table.Column<int>(type: "INTEGER", nullable: false),
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
                    table.PrimaryKey("PK_NotificationProviderConfigs", x => x.CtrlNbr);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationProviderConfigs");
        }
    }
}
