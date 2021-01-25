using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PcrBattleChannel.Migrations
{
    public partial class AddComboTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastZhouUpdate",
                table: "Guilds",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastComboUpdate",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastZhouUpdate",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "LastComboUpdate",
                table: "AspNetUsers");
        }
    }
}
