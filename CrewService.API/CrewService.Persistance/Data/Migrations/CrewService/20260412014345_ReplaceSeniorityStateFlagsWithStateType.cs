using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class ReplaceSeniorityStateFlagsWithStateType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Active",
                table: "SeniorityStates");

            migrationBuilder.DropColumn(
                name: "CutBack",
                table: "SeniorityStates");

            migrationBuilder.DropColumn(
                name: "Inactive",
                table: "SeniorityStates");

            migrationBuilder.AddColumn<string>(
                name: "StateType",
                table: "SeniorityStates",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StateType",
                table: "SeniorityStates");

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "SeniorityStates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CutBack",
                table: "SeniorityStates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Inactive",
                table: "SeniorityStates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
