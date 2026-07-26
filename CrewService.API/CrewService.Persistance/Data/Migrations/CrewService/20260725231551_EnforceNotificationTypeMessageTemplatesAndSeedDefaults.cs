using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class EnforceNotificationTypeMessageTemplatesAndSeedDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MessageTemplate",
                table: "NotificationTypeConfigs",
                type: "TEXT",
                maxLength: 2000,
                nullable: false,
                defaultValue: "{message}",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 2000,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MessageTemplate",
                table: "NotificationTypeConfigs",
                type: "TEXT",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 2000);
        }
    }
}
