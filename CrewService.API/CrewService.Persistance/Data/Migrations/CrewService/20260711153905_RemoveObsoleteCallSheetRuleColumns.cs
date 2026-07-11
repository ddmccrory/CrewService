using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class RemoveObsoleteCallSheetRuleColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnchorType",
                table: "CallSheetRule");

            migrationBuilder.DropColumn(
                name: "PostAnchorOffsetMinutes",
                table: "CallSheetRule");

            migrationBuilder.DropColumn(
                name: "SpecialPatterns",
                table: "CallSheetRule");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AnchorType",
                table: "CallSheetRule",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "PostAnchorOffsetMinutes",
                table: "CallSheetRule",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SpecialPatterns",
                table: "CallSheetRule",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
