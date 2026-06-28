using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddWillWorkToSeniorityMove : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WillWork",
                table: "SeniorityMoves",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WillWorkEnabled",
                table: "SeniorityMovePolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WillWork",
                table: "SeniorityMoves");

            migrationBuilder.DropColumn(
                name: "WillWorkEnabled",
                table: "SeniorityMovePolicies");
        }
    }
}
