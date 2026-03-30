using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddDepartmentToCrew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DepartmentCtrlNbr",
                table: "Crews",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Crews_DepartmentCtrlNbr",
                table: "Crews",
                column: "DepartmentCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_Crews_Department_DepartmentCtrlNbr",
                table: "Crews",
                column: "DepartmentCtrlNbr",
                principalTable: "Department",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Crews_Department_DepartmentCtrlNbr",
                table: "Crews");

            migrationBuilder.DropIndex(
                name: "IX_Crews_DepartmentCtrlNbr",
                table: "Crews");

            migrationBuilder.DropColumn(
                name: "DepartmentCtrlNbr",
                table: "Crews");
        }
    }
}
