using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddAbsenceApprovalPolicyAutoMarkOffThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutoMarkOffIfWithinHours",
                table: "AbsenceApprovalPolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "AutoMarkOffIfWithinHoursEnabled",
                table: "AbsenceApprovalPolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoMarkOffIfWithinHours",
                table: "AbsenceApprovalPolicies");

            migrationBuilder.DropColumn(
                name: "AutoMarkOffIfWithinHoursEnabled",
                table: "AbsenceApprovalPolicies");
        }
    }
}
