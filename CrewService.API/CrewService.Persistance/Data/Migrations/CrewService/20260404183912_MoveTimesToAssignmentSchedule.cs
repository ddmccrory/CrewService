using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class MoveTimesToAssignmentSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShiftEndUtc",
                table: "ShiftInstances");

            migrationBuilder.DropColumn(
                name: "ShiftStartUtc",
                table: "ShiftInstances");

            migrationBuilder.DropColumn(
                name: "DefaultEndTime",
                table: "ShiftDefinitions");

            migrationBuilder.DropColumn(
                name: "DefaultStartTime",
                table: "ShiftDefinitions");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "OffDutyTime",
                table: "AssignmentSchedules",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "OnDutyTime",
                table: "AssignmentSchedules",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OffDutyTime",
                table: "AssignmentSchedules");

            migrationBuilder.DropColumn(
                name: "OnDutyTime",
                table: "AssignmentSchedules");

            migrationBuilder.AddColumn<DateTime>(
                name: "ShiftEndUtc",
                table: "ShiftInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "ShiftStartUtc",
                table: "ShiftInstances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DefaultEndTime",
                table: "ShiftDefinitions",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "DefaultStartTime",
                table: "ShiftDefinitions",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));
        }
    }
}
