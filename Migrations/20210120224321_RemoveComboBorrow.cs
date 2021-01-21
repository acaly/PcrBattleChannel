using Microsoft.EntityFrameworkCore.Migrations;

namespace PcrBattleChannel.Migrations
{
    public partial class RemoveComboBorrow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Zhou1Borrow",
                table: "UserCombos");

            migrationBuilder.DropColumn(
                name: "Zhou2Borrow",
                table: "UserCombos");

            migrationBuilder.DropColumn(
                name: "Zhou3Borrow",
                table: "UserCombos");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Zhou1Borrow",
                table: "UserCombos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Zhou2Borrow",
                table: "UserCombos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Zhou3Borrow",
                table: "UserCombos",
                type: "int",
                nullable: true);
        }
    }
}
