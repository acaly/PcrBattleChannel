using Microsoft.EntityFrameworkCore.Migrations;

namespace PcrBattleChannel.Migrations
{
    public partial class AddComboBorrow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BorrowInfo",
                table: "UserCombos",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BorrowInfo",
                table: "UserCombos");
        }
    }
}
