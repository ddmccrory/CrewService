using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddEffectiveDateStrategyToSeniorityMovePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CrewToBoardStrategy",
                table: "SeniorityMovePolicies",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CrewToCrewStrategy",
                table: "SeniorityMovePolicies",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExtendedAbsenceToCrewStrategy",
                table: "SeniorityMovePolicies",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExtraBoardToCrewStrategy",
                table: "SeniorityMovePolicies",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HangoutToCrewStrategy",
                table: "SeniorityMovePolicies",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NewHireToCrewStrategy",
                table: "SeniorityMovePolicies",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TrainingToCrewStrategy",
                table: "SeniorityMovePolicies",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CrewToBoardStrategy",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropColumn(
                name: "CrewToCrewStrategy",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropColumn(
                name: "ExtendedAbsenceToCrewStrategy",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropColumn(
                name: "ExtraBoardToCrewStrategy",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropColumn(
                name: "HangoutToCrewStrategy",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropColumn(
                name: "NewHireToCrewStrategy",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropColumn(
                name: "TrainingToCrewStrategy",
                table: "SeniorityMovePolicies");
        }
    }
}
