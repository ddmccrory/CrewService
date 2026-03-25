using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddPermissionCraftCtrlNbr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Permissions_RoleCtrlNbr_FeatureCtrlNbr_ParentCtrlNbr",
                table: "Permissions");

            migrationBuilder.AddColumn<long>(
                name: "CraftCtrlNbr",
                table: "Permissions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_CraftCtrlNbr",
                table: "Permissions",
                column: "CraftCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_RoleCtrlNbr_FeatureCtrlNbr_ParentCtrlNbr_CraftCtrlNbr",
                table: "Permissions",
                columns: new[] { "RoleCtrlNbr", "FeatureCtrlNbr", "ParentCtrlNbr", "CraftCtrlNbr" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Crafts_CraftCtrlNbr",
                table: "Permissions",
                column: "CraftCtrlNbr",
                principalTable: "Crafts",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Crafts_CraftCtrlNbr",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_CraftCtrlNbr",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_RoleCtrlNbr_FeatureCtrlNbr_ParentCtrlNbr_CraftCtrlNbr",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "CraftCtrlNbr",
                table: "Permissions");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_RoleCtrlNbr_FeatureCtrlNbr_ParentCtrlNbr",
                table: "Permissions",
                columns: new[] { "RoleCtrlNbr", "FeatureCtrlNbr", "ParentCtrlNbr" },
                unique: true);
        }
    }
}
