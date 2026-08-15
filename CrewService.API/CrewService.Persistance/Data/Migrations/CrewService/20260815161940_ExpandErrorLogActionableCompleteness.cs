using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class ExpandErrorLogActionableCompleteness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ErrorKind",
                table: "ErrorLogs",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "UnhandledException");

            migrationBuilder.AddColumn<string>(
                name: "FingerprintHash",
                table: "ErrorLogs",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "LEGACY");

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstOccurredAtUtc",
                table: "ErrorLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOccurredAtUtc",
                table: "ErrorLogs",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "OccurrenceCount",
                table: "ErrorLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolvedAtUtc",
                table: "ErrorLogs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolvedBy",
                table: "ErrorLogs",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ErrorLogs",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "New");

            migrationBuilder.AddColumn<string>(
                name: "SuppressionReason",
                table: "ErrorLogs",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ErrorLogs
                SET FirstOccurredAtUtc = OccurredAtUtc,
                    LastOccurredAtUtc = OccurredAtUtc,
                    OccurrenceCount = CASE WHEN OccurrenceCount < 1 THEN 1 ELSE OccurrenceCount END,
                    ErrorKind = CASE WHEN ErrorKind = '' THEN 'UnhandledException' ELSE ErrorKind END,
                    Status = CASE WHEN Status = '' THEN 'New' ELSE Status END;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_ErrorKind",
                table: "ErrorLogs",
                column: "ErrorKind");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_FingerprintHash",
                table: "ErrorLogs",
                column: "FingerprintHash");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_FingerprintHash_Status",
                table: "ErrorLogs",
                columns: new[] { "FingerprintHash", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_Status",
                table: "ErrorLogs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ErrorLogs_ErrorKind",
                table: "ErrorLogs");

            migrationBuilder.DropIndex(
                name: "IX_ErrorLogs_FingerprintHash",
                table: "ErrorLogs");

            migrationBuilder.DropIndex(
                name: "IX_ErrorLogs_FingerprintHash_Status",
                table: "ErrorLogs");

            migrationBuilder.DropIndex(
                name: "IX_ErrorLogs_Status",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "ErrorKind",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "FingerprintHash",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "FirstOccurredAtUtc",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "LastOccurredAtUtc",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "OccurrenceCount",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "ResolvedAtUtc",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "ResolvedBy",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "SuppressionReason",
                table: "ErrorLogs");
        }
    }
}
