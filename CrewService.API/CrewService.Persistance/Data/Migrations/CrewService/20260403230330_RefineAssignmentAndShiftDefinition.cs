using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RefineAssignmentAndShiftDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnDutyTime",
                table: "Assignments");

            migrationBuilder.AddColumn<long>(
                name: "DepartmentCtrlNbr",
                table: "ShiftDefinitions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftDefinitions_DepartmentCtrlNbr",
                table: "ShiftDefinitions",
                column: "DepartmentCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftDefinitions_Department_DepartmentCtrlNbr",
                table: "ShiftDefinitions",
                column: "DepartmentCtrlNbr",
                principalTable: "Department",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShiftDefinitions_Department_DepartmentCtrlNbr",
                table: "ShiftDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ShiftDefinitions_DepartmentCtrlNbr",
                table: "ShiftDefinitions");

            migrationBuilder.DropColumn(
                name: "DepartmentCtrlNbr",
                table: "ShiftDefinitions");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "OnDutyTime",
                table: "Assignments",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));
        }
    }
}
