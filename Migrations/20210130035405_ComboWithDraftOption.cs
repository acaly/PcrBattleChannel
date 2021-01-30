using Microsoft.EntityFrameworkCore.Migrations;

namespace PcrBattleChannel.Migrations
{
    public partial class ComboWithDraftOption : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ComboIncludesDrafts",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComboIncludesDrafts",
                table: "AspNetUsers");
        }
    }
}
