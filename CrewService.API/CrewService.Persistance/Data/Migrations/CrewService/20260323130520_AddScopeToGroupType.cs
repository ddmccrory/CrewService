using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddScopeToGroupType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GroupTypes_Name",
                table: "GroupTypes");

            migrationBuilder.AddColumn<long>(
                name: "ParentCtrlNbr",
                table: "GroupTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "RailroadCtrlNbr",
                table: "GroupTypes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_GroupTypes_Name_ParentCtrlNbr_RailroadCtrlNbr",
                table: "GroupTypes",
                columns: new[] { "Name", "ParentCtrlNbr", "RailroadCtrlNbr" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GroupTypes_Name_ParentCtrlNbr_RailroadCtrlNbr",
                table: "GroupTypes");

            migrationBuilder.DropColumn(
                name: "ParentCtrlNbr",
                table: "GroupTypes");

            migrationBuilder.DropColumn(
                name: "RailroadCtrlNbr",
                table: "GroupTypes");

            migrationBuilder.CreateIndex(
                name: "IX_GroupTypes_Name",
                table: "GroupTypes",
                column: "Name",
                unique: true);
        }
    }
}
