using Microsoft.EntityFrameworkCore.Migrations;

namespace PcrBattleChannel.Migrations
{
    public partial class RemoveUserTablePlanId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextAttemptPlanIndex",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<bool>(
                name: "IsStatusGuessed",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStatusGuessed",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<int>(
                name: "NextAttemptPlanIndex",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
