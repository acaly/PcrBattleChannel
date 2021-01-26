using Microsoft.EntityFrameworkCore.Migrations;

namespace PcrBattleChannel.Migrations
{
    public partial class FixZhouDeletionAgain : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCombos_UserZhouVariants_Zhou1ID",
                table: "UserCombos");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCombos_UserZhouVariants_Zhou2ID",
                table: "UserCombos");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCombos_UserZhouVariants_Zhou3ID",
                table: "UserCombos");

            migrationBuilder.DropForeignKey(
                name: "FK_ZhouVariantCharacterConfigs_ZhouVariants_ZhouVariantID1",
                table: "ZhouVariantCharacterConfigs");

            migrationBuilder.DropIndex(
                name: "IX_ZhouVariantCharacterConfigs_ZhouVariantID1",
                table: "ZhouVariantCharacterConfigs");

            migrationBuilder.DropColumn(
                name: "ZhouVariantID1",
                table: "ZhouVariantCharacterConfigs");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCombos_UserZhouVariants_Zhou1ID",
                table: "UserCombos",
                column: "Zhou1ID",
                principalTable: "UserZhouVariants",
                principalColumn: "UserZhouVariantID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCombos_UserZhouVariants_Zhou2ID",
                table: "UserCombos",
                column: "Zhou2ID",
                principalTable: "UserZhouVariants",
                principalColumn: "UserZhouVariantID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCombos_UserZhouVariants_Zhou3ID",
                table: "UserCombos",
                column: "Zhou3ID",
                principalTable: "UserZhouVariants",
                principalColumn: "UserZhouVariantID",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCombos_UserZhouVariants_Zhou1ID",
                table: "UserCombos");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCombos_UserZhouVariants_Zhou2ID",
                table: "UserCombos");

            migrationBuilder.DropForeignKey(
                name: "FK_UserCombos_UserZhouVariants_Zhou3ID",
                table: "UserCombos");

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
                name: "FK_UserCombos_UserZhouVariants_Zhou1ID",
                table: "UserCombos",
                column: "Zhou1ID",
                principalTable: "UserZhouVariants",
                principalColumn: "UserZhouVariantID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCombos_UserZhouVariants_Zhou2ID",
                table: "UserCombos",
                column: "Zhou2ID",
                principalTable: "UserZhouVariants",
                principalColumn: "UserZhouVariantID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserCombos_UserZhouVariants_Zhou3ID",
                table: "UserCombos",
                column: "Zhou3ID",
                principalTable: "UserZhouVariants",
                principalColumn: "UserZhouVariantID",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ZhouVariantCharacterConfigs_ZhouVariants_ZhouVariantID1",
                table: "ZhouVariantCharacterConfigs",
                column: "ZhouVariantID1",
                principalTable: "ZhouVariants",
                principalColumn: "ZhouVariantID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
