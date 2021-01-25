using Microsoft.EntityFrameworkCore.Migrations;

namespace PcrBattleChannel.Migrations
{
    public partial class CascadeDeleteZhouVariant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ZhouVariantID1",
                table: "ZhouVariantCharacterConfigs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ZhouVariantCharacterConfigs_ZhouVariantID1",
                table: "ZhouVariantCharacterConfigs",
                column: "ZhouVariantID1");

            migrationBuilder.AddForeignKey(
                name: "FK_ZhouVariantCharacterConfigs_ZhouVariants_ZhouVariantID1",
                table: "ZhouVariantCharacterConfigs",
                column: "ZhouVariantID1",
                principalTable: "ZhouVariants",
                principalColumn: "ZhouVariantID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ZhouVariantCharacterConfigs_ZhouVariants_ZhouVariantID1",
                table: "ZhouVariantCharacterConfigs");

            migrationBuilder.DropIndex(
                name: "IX_ZhouVariantCharacterConfigs_ZhouVariantID1",
                table: "ZhouVariantCharacterConfigs");

            migrationBuilder.DropColumn(
                name: "ZhouVariantID1",
                table: "ZhouVariantCharacterConfigs");
        }
    }
}
