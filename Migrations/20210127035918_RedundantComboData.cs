using Microsoft.EntityFrameworkCore.Migrations;

namespace PcrBattleChannel.Migrations
{
    public partial class RedundantComboData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Boss1",
                table: "UserCombos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Boss2",
                table: "UserCombos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Boss3",
                table: "UserCombos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Damage1",
                table: "UserCombos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Damage2",
                table: "UserCombos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Damage3",
                table: "UserCombos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Boss1",
                table: "UserCombos");

            migrationBuilder.DropColumn(
                name: "Boss2",
                table: "UserCombos");

            migrationBuilder.DropColumn(
                name: "Boss3",
                table: "UserCombos");

            migrationBuilder.DropColumn(
                name: "Damage1",
                table: "UserCombos");

            migrationBuilder.DropColumn(
                name: "Damage2",
                table: "UserCombos");

            migrationBuilder.DropColumn(
                name: "Damage3",
                table: "UserCombos");
        }
    }
}
