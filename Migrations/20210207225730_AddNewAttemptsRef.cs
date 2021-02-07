using Microsoft.EntityFrameworkCore.Migrations;

namespace PcrBattleChannel.Migrations
{
    public partial class AddNewAttemptsRef : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Attempt1Borrow",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Attempt1ID",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Attempt2Borrow",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Attempt2ID",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Attempt3Borrow",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Attempt3ID",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Attempt1ID",
                table: "AspNetUsers",
                column: "Attempt1ID");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Attempt2ID",
                table: "AspNetUsers",
                column: "Attempt2ID");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Attempt3ID",
                table: "AspNetUsers",
                column: "Attempt3ID");

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

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Attempt1ID",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Attempt2ID",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Attempt3ID",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Attempt1Borrow",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Attempt1ID",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Attempt2Borrow",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Attempt2ID",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Attempt3Borrow",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Attempt3ID",
                table: "AspNetUsers");
        }
    }
}
