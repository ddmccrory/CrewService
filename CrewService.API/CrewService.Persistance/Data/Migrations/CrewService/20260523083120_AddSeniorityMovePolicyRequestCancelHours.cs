using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddSeniorityMovePolicyRequestCancelHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeniorityBasis",
                table: "SeniorityMovePolicies");

            migrationBuilder.AddColumn<int>(
                name: "CancelHours",
                table: "SeniorityMovePolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RequestHours",
                table: "SeniorityMovePolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelHours",
                table: "SeniorityMovePolicies");

            migrationBuilder.DropColumn(
                name: "RequestHours",
                table: "SeniorityMovePolicies");

            migrationBuilder.AddColumn<string>(
                name: "SeniorityBasis",
                table: "SeniorityMovePolicies",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}
