using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOnDutyCompletionAndOffDutyConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAtUtc",
                table: "OnDutyRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompletionStatus",
                table: "OnDutyRecords",
                type: "TEXT",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "OffDutyTimeConfirmed",
                table: "OffDutyRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OffDutyTimeConfirmedAtUtc",
                table: "OffDutyRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OffDutyTimeConfirmedBy",
                table: "OffDutyRecords",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "OnDutyRecords");

            migrationBuilder.DropColumn(
                name: "CompletionStatus",
                table: "OnDutyRecords");

            migrationBuilder.DropColumn(
                name: "OffDutyTimeConfirmed",
                table: "OffDutyRecords");

            migrationBuilder.DropColumn(
                name: "OffDutyTimeConfirmedAtUtc",
                table: "OffDutyRecords");

            migrationBuilder.DropColumn(
                name: "OffDutyTimeConfirmedBy",
                table: "OffDutyRecords");
        }
    }
}
