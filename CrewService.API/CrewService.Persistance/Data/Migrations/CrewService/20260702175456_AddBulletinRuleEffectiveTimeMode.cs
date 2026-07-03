using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddBulletinRuleEffectiveTimeMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EffectiveTimeMode",
                table: "BulletinRules",
                type: "TEXT",
                maxLength: 40,
                nullable: false,
                defaultValue: "FixedEffectiveTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EffectiveTimeMode",
                table: "BulletinRules");
        }
    }
}
