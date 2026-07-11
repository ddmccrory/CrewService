using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddTransitionEligibilityDaysToSeniorityMovePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EligibilityDays",
                table: "SeniorityMovePolicies",
                newName: "TrainingToCrewEligibilityDays");

            migrationBuilder.AddColumn<int>(
                name: "CrewToBoardEligibilityDays",
                table: "SeniorityMovePolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CrewToCrewEligibilityDays",
                table: "SeniorityMovePolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExtendedAbsenceToCrewEligibilityDays",
                table: "SeniorityMovePolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExtraBoardToCrewEligibilityDays",
                table: "SeniorityMovePolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HangoutToCrewEligibilityDays",
                table: "SeniorityMovePolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NewHireToCrewEligibilityDays",
                table: "SeniorityMovePolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CrewToBoardEligibilityDays",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropColumn(
                name: "CrewToCrewEligibilityDays",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropColumn(
                name: "ExtendedAbsenceToCrewEligibilityDays",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropColumn(
                name: "ExtraBoardToCrewEligibilityDays",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropColumn(
                name: "HangoutToCrewEligibilityDays",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropColumn(
                name: "NewHireToCrewEligibilityDays",
                table: "SeniorityMovePolicies");

            migrationBuilder.RenameColumn(
                name: "TrainingToCrewEligibilityDays",
                table: "SeniorityMovePolicies",
                newName: "EligibilityDays");
        }
    }
}
