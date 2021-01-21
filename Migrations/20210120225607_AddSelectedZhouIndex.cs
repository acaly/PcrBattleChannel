using Microsoft.EntityFrameworkCore.Migrations;

namespace PcrBattleChannel.Migrations
{
    public partial class AddSelectedZhouIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSelected",
                table: "UserCombos");

            migrationBuilder.AddColumn<int>(
                name: "SelectedZhou",
                table: "UserCombos",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedZhou",
                table: "UserCombos");

            migrationBuilder.AddColumn<bool>(
                name: "IsSelected",
                table: "UserCombos",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
