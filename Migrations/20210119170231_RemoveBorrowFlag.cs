using Microsoft.EntityFrameworkCore.Migrations;

namespace PcrBattleChannel.Migrations
{
    public partial class RemoveBorrowFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowBorrow",
                table: "ZhouVariantCharacterConfigs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowBorrow",
                table: "ZhouVariantCharacterConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
