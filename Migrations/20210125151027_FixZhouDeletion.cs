using Microsoft.EntityFrameworkCore.Migrations;

namespace PcrBattleChannel.Migrations
{
    public partial class FixZhouDeletion : Migration
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
    }
}
