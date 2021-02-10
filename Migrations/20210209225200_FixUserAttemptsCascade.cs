using Microsoft.EntityFrameworkCore.Migrations;

namespace PcrBattleChannel.Migrations
{
    public partial class FixUserAttemptsCascade : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_ZhouVariants_Attempt1ID",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_ZhouVariants_Attempt2ID",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_ZhouVariants_Attempt3ID",
                table: "AspNetUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_ZhouVariants_Attempt1ID",
                table: "AspNetUsers",
                column: "Attempt1ID",
                principalTable: "ZhouVariants",
                principalColumn: "ZhouVariantID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_ZhouVariants_Attempt2ID",
                table: "AspNetUsers",
                column: "Attempt2ID",
                principalTable: "ZhouVariants",
                principalColumn: "ZhouVariantID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_ZhouVariants_Attempt3ID",
                table: "AspNetUsers",
                column: "Attempt3ID",
                principalTable: "ZhouVariants",
                principalColumn: "ZhouVariantID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_ZhouVariants_Attempt1ID",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_ZhouVariants_Attempt2ID",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_ZhouVariants_Attempt3ID",
                table: "AspNetUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_ZhouVariants_Attempt1ID",
                table: "AspNetUsers",
                column: "Attempt1ID",
                principalTable: "ZhouVariants",
                principalColumn: "ZhouVariantID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_ZhouVariants_Attempt2ID",
                table: "AspNetUsers",
                column: "Attempt2ID",
                principalTable: "ZhouVariants",
                principalColumn: "ZhouVariantID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_ZhouVariants_Attempt3ID",
                table: "AspNetUsers",
                column: "Attempt3ID",
                principalTable: "ZhouVariants",
                principalColumn: "ZhouVariantID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
