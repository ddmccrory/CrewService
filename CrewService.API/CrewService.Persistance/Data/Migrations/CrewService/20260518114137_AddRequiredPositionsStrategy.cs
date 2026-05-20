using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CrewService.Persistance.Data.Migrations.CrewService
{
    /// <inheritdoc />
    public partial class AddRequiredPositionsStrategy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RequiredPositionsStrategyCtrlNbr",
                table: "RosterBoards",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RequiredPositionsStrategy",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FormulaType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ParametersJson = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false, defaultValue: "{}"),
                    RailroadCtrlNbr = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequiredPositionsStrategy", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_RequiredPositionsStrategy_DynamicGroups_RailroadCtrlNbr",
                        column: x => x.RailroadCtrlNbr,
                        principalTable: "DynamicGroups",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CraftRequiredPositionsStrategy",
                columns: table => new
                {
                    CtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CraftCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    StrategyCtrlNbr = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CreatedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ModifiedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy_AuditName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DeletedBy_AuditDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CraftRequiredPositionsStrategy", x => x.CtrlNbr);
                    table.ForeignKey(
                        name: "FK_CraftRequiredPositionsStrategy_Crafts_CraftCtrlNbr",
                        column: x => x.CraftCtrlNbr,
                        principalTable: "Crafts",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CraftRequiredPositionsStrategy_RequiredPositionsStrategy_StrategyCtrlNbr",
                        column: x => x.StrategyCtrlNbr,
                        principalTable: "RequiredPositionsStrategy",
                        principalColumn: "CtrlNbr",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RosterBoards_RequiredPositionsStrategyCtrlNbr",
                table: "RosterBoards",
                column: "RequiredPositionsStrategyCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_CraftRequiredPositionsStrategy_CraftCtrlNbr",
                table: "CraftRequiredPositionsStrategy",
                column: "CraftCtrlNbr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CraftRequiredPositionsStrategy_StrategyCtrlNbr",
                table: "CraftRequiredPositionsStrategy",
                column: "StrategyCtrlNbr");

            migrationBuilder.CreateIndex(
                name: "IX_RequiredPositionsStrategy_Code_RailroadCtrlNbr",
                table: "RequiredPositionsStrategy",
                columns: new[] { "Code", "RailroadCtrlNbr" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequiredPositionsStrategy_RailroadCtrlNbr",
                table: "RequiredPositionsStrategy",
                column: "RailroadCtrlNbr");

            migrationBuilder.AddForeignKey(
                name: "FK_RosterBoards_RequiredPositionsStrategy_RequiredPositionsStrategyCtrlNbr",
                table: "RosterBoards",
                column: "RequiredPositionsStrategyCtrlNbr",
                principalTable: "RequiredPositionsStrategy",
                principalColumn: "CtrlNbr",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RosterBoards_RequiredPositionsStrategy_RequiredPositionsStrategyCtrlNbr",
                table: "RosterBoards");

            migrationBuilder.DropTable(
                name: "CraftRequiredPositionsStrategy");

            migrationBuilder.DropTable(
                name: "RequiredPositionsStrategy");

            migrationBuilder.DropIndex(
                name: "IX_RosterBoards_RequiredPositionsStrategyCtrlNbr",
                table: "RosterBoards");

            migrationBuilder.DropColumn(
                name: "RequiredPositionsStrategyCtrlNbr",
                table: "RosterBoards");
        }
    }
}
