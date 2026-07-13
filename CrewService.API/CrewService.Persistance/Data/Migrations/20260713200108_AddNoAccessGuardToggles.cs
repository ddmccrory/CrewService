using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNoAccessGuardToggles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BlockIfEmployeeMarkedOff",
                table: "NoAccessPolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BlockIfLastVacatedIncumbent",
                table: "NoAccessPolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockIfEmployeeMarkedOff",
                table: "NoAccessPolicies");

            migrationBuilder.DropColumn(
                name: "BlockIfLastVacatedIncumbent",
                table: "NoAccessPolicies");
        }
    }
}
