using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddHangoutAutoMovePolicyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HangoutAutoMoveDelayHours",
                table: "CraftOperationsPolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HangoutAutoMoveEnabled",
                table: "CraftOperationsPolicies",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HangoutAutoMoveTargetBoardType",
                table: "CraftOperationsPolicies",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HangoutAutoMoveDelayHours",
                table: "CraftOperationsPolicies");

            migrationBuilder.DropColumn(
                name: "HangoutAutoMoveEnabled",
                table: "CraftOperationsPolicies");

            migrationBuilder.DropColumn(
                name: "HangoutAutoMoveTargetBoardType",
                table: "CraftOperationsPolicies");
        }
    }
}
