using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.Identity
{
    /// <inheritdoc />
    public partial class EnforceEmployeeLinkedUserNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_AspNetUsers_EmployeeName_Required",
                table: "AspNetUsers",
                sql: "[EmployeeNumber] IS NULL OR ([FirstName] IS NOT NULL AND [FirstName] <> '' AND [LastName] IS NOT NULL AND [LastName] <> '')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AspNetUsers_EmployeeName_Required",
                table: "AspNetUsers");
        }
    }
}
