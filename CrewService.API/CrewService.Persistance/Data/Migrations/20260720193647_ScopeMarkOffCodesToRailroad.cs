using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class ScopeMarkOffCodesToRailroad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AbsenceCodes_Code",
                table: "AbsenceCodes");

            migrationBuilder.AddColumn<long>(
                name: "RailroadCtrlNbr",
                table: "AbsenceCodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceCodes_RailroadCtrlNbr_Code",
                table: "AbsenceCodes",
                columns: new[] { "RailroadCtrlNbr", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AbsenceCodes_RailroadCtrlNbr_Code",
                table: "AbsenceCodes");

            migrationBuilder.DropColumn(
                name: "RailroadCtrlNbr",
                table: "AbsenceCodes");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceCodes_Code",
                table: "AbsenceCodes",
                column: "Code",
                unique: true);
        }
    }
}
