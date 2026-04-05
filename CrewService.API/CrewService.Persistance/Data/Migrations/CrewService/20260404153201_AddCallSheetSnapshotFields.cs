using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddCallSheetSnapshotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DepartmentCtrlNbr",
                table: "ShiftInstances",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepartmentName",
                table: "ShiftInstances",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShiftDisplayName",
                table: "ShiftInstances",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentCode",
                table: "PositionSlotInstances",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "AssignmentCtrlNbr",
                table: "PositionSlotInstances",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "AssignmentName",
                table: "PositionSlotInstances",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CraftRoleName",
                table: "PositionSlotInstances",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartmentCtrlNbr",
                table: "ShiftInstances");

            migrationBuilder.DropColumn(
                name: "DepartmentName",
                table: "ShiftInstances");

            migrationBuilder.DropColumn(
                name: "ShiftDisplayName",
                table: "ShiftInstances");

            migrationBuilder.DropColumn(
                name: "AssignmentCode",
                table: "PositionSlotInstances");

            migrationBuilder.DropColumn(
                name: "AssignmentCtrlNbr",
                table: "PositionSlotInstances");

            migrationBuilder.DropColumn(
                name: "AssignmentName",
                table: "PositionSlotInstances");

            migrationBuilder.DropColumn(
                name: "CraftRoleName",
                table: "PositionSlotInstances");
        }
    }
}
