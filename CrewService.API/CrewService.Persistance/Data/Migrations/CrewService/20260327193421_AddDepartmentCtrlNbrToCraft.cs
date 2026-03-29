using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddDepartmentCtrlNbrToCraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DepartmentCtrlNbr",
                table: "Crafts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Crafts_DepartmentCtrlNbr",
                table: "Crafts",
                column: "DepartmentCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_Crafts_Department_DepartmentCtrlNbr",
                table: "Crafts",
                column: "DepartmentCtrlNbr",
                principalTable: "Department",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Crafts_Department_DepartmentCtrlNbr",
                table: "Crafts");

            migrationBuilder.DropIndex(
                name: "IX_Crafts_DepartmentCtrlNbr",
                table: "Crafts");

            migrationBuilder.DropColumn(
                name: "DepartmentCtrlNbr",
                table: "Crafts");
        }
    }
}
