using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class SystemLevelStrategies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequiredPositionsStrategy_DynamicGroups_RailroadCtrlNbr",
                table: "RequiredPositionsStrategy");

            migrationBuilder.DropIndex(
                name: "IX_RequiredPositionsStrategy_Code_RailroadCtrlNbr",
                table: "RequiredPositionsStrategy");

            migrationBuilder.DropIndex(
                name: "IX_RequiredPositionsStrategy_RailroadCtrlNbr",
                table: "RequiredPositionsStrategy");

            migrationBuilder.DropColumn(
                name: "RailroadCtrlNbr",
                table: "RequiredPositionsStrategy");

            migrationBuilder.AddColumn<string>(
                name: "ParametersJson",
                table: "CraftRequiredPositionsStrategy",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequiredPositionsStrategy_Code",
                table: "RequiredPositionsStrategy",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RequiredPositionsStrategy_Code",
                table: "RequiredPositionsStrategy");

            migrationBuilder.DropColumn(
                name: "ParametersJson",
                table: "CraftRequiredPositionsStrategy");

            migrationBuilder.AddColumn<long>(
                name: "RailroadCtrlNbr",
                table: "RequiredPositionsStrategy",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequiredPositionsStrategy_Code_RailroadCtrlNbr",
                table: "RequiredPositionsStrategy",
                columns: new[] { "Code", "RailroadCtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequiredPositionsStrategy_RailroadCtrlNbr",
                table: "RequiredPositionsStrategy",
                column: "RailroadCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_RequiredPositionsStrategy_DynamicGroups_RailroadCtrlNbr",
                table: "RequiredPositionsStrategy",
                column: "RailroadCtrlNbr",
                principalTable: "DynamicGroups",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
