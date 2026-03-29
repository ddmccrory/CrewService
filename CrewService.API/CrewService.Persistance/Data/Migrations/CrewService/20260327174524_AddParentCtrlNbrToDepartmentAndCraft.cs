using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddParentCtrlNbrToDepartmentAndCraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Department_DynamicGroups_RailroadCtrlNbr",
                table: "Department");

            migrationBuilder.DropIndex(
                name: "IX_Department_RailroadCtrlNbr",
                table: "Department");

            migrationBuilder.RenameColumn(
                name: "RailroadCtrlNbr",
                table: "Department",
                newName: "ParentCtrlNbr");

            migrationBuilder.AddColumn<long>(
                name: "DynamicGroupCtrlNbr",
                table: "Department",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "DynamicGroupCtrlNbr",
                table: "Crafts",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<long>(
                name: "ParentCtrlNbr",
                table: "Crafts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Department_DynamicGroupCtrlNbr",
                table: "Department",
                column: "DynamicGroupCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_Department_DynamicGroups_DynamicGroupCtrlNbr",
                table: "Department",
                column: "DynamicGroupCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Department_DynamicGroups_DynamicGroupCtrlNbr",
                table: "Department");

            migrationBuilder.DropIndex(
                name: "IX_Department_DynamicGroupCtrlNbr",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "DynamicGroupCtrlNbr",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "ParentCtrlNbr",
                table: "Crafts");

            migrationBuilder.RenameColumn(
                name: "ParentCtrlNbr",
                table: "Department",
                newName: "RailroadCtrlNbr");

            migrationBuilder.AlterColumn<long>(
                name: "DynamicGroupCtrlNbr",
                table: "Crafts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Department_RailroadCtrlNbr",
                table: "Department",
                column: "RailroadCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_Department_DynamicGroups_RailroadCtrlNbr",
                table: "Department",
                column: "RailroadCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
