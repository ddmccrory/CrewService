using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class ConvertScopeCtrlNbrsToNullableControlNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "RailroadCtrlNbr",
                table: "GroupTypes",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<long>(
                name: "ParentGroupTypeCtrlNbr",
                table: "GroupTypes",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<long>(
                name: "ParentCtrlNbr",
                table: "GroupTypes",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<long>(
                name: "RailroadCtrlNbr",
                table: "DynamicGroups",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<long>(
                name: "ParentCtrlNbr",
                table: "DynamicGroups",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<long>(
                name: "ParentCtrlNbr",
                table: "Department",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<long>(
                name: "ParentCtrlNbr",
                table: "Crafts",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");
            // Data migration: convert 0 sentinel values to NULL
            migrationBuilder.Sql("UPDATE GroupTypes SET ParentCtrlNbr = NULL WHERE ParentCtrlNbr = 0;");
            migrationBuilder.Sql("UPDATE GroupTypes SET RailroadCtrlNbr = NULL WHERE RailroadCtrlNbr = 0;");
            migrationBuilder.Sql("UPDATE GroupTypes SET ParentGroupTypeCtrlNbr = NULL WHERE ParentGroupTypeCtrlNbr = 0;");
            migrationBuilder.Sql("UPDATE DynamicGroups SET ParentCtrlNbr = NULL WHERE ParentCtrlNbr = 0;");
            migrationBuilder.Sql("UPDATE DynamicGroups SET RailroadCtrlNbr = NULL WHERE RailroadCtrlNbr = 0;");
            migrationBuilder.Sql("UPDATE Department SET ParentCtrlNbr = NULL WHERE ParentCtrlNbr = 0;");
            migrationBuilder.Sql("UPDATE Crafts SET ParentCtrlNbr = NULL WHERE ParentCtrlNbr = 0;");
            migrationBuilder.Sql("UPDATE Permissions SET ParentCtrlNbr = NULL WHERE ParentCtrlNbr = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "RailroadCtrlNbr",
                table: "GroupTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ParentGroupTypeCtrlNbr",
                table: "GroupTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ParentCtrlNbr",
                table: "GroupTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "RailroadCtrlNbr",
                table: "DynamicGroups",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ParentCtrlNbr",
                table: "DynamicGroups",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ParentCtrlNbr",
                table: "Department",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "ParentCtrlNbr",
                table: "Crafts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
