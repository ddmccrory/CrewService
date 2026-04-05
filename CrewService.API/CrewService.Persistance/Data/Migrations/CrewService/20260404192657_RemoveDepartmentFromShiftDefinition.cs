using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RemoveDepartmentFromShiftDefinition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
