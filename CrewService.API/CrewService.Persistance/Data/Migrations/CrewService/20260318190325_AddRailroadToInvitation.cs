using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddRailroadToInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RailroadCtrlNbr",
                table: "Invitations",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_RailroadCtrlNbr",
                table: "Invitations",
                column: "RailroadCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Railroads_RailroadCtrlNbr",
                table: "Invitations",
                column: "RailroadCtrlNbr",
                principalTable: "Railroads",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Railroads_RailroadCtrlNbr",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_RailroadCtrlNbr",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "RailroadCtrlNbr",
                table: "Invitations");
        }
    }
}
