using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PcrBattleChannel.Migrations
{
    public partial class DeleteIMFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildBossStatuses");

            migrationBuilder.DropTable(
                name: "UserCombos");

            migrationBuilder.DropTable(
                name: "UserZhouVariants");

            migrationBuilder.DropColumn(
                name: "IsUsed",
                table: "UserCharacterStatuses");

            migrationBuilder.DropColumn(
                name: "LastCalculation",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "LastYobotSync",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "PredictBossIndex",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "IsValueApproximate",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastComboUpdate",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "PredictBossDamageRatio",
                table: "Guilds",
                newName: "DamageCoefficient");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DamageCoefficient",
                table: "Guilds",
                newName: "PredictBossDamageRatio");

            migrationBuilder.AddColumn<bool>(
                name: "IsUsed",
                table: "UserCharacterStatuses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCalculation",
                table: "Guilds",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastYobotSync",
                table: "Guilds",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "PredictBossIndex",
                table: "Guilds",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsValueApproximate",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastComboUpdate",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "GuildBossStatuses",
                columns: table => new
                {
                    GuildBossStatusID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BossID = table.Column<int>(type: "INTEGER", nullable: false),
                    DamageRatio = table.Column<float>(type: "REAL", nullable: false),
                    DisplayColumn = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayRow = table.Column<int>(type: "INTEGER", nullable: false),
                    GuildID = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPlan = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildBossStatuses", x => x.GuildBossStatusID);
                    table.ForeignKey(
                        name: "FK_GuildBossStatuses_Bosses_BossID",
                        column: x => x.BossID,
                        principalTable: "Bosses",
                        principalColumn: "BossID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuildBossStatuses_Guilds_GuildID",
                        column: x => x.GuildID,
                        principalTable: "Guilds",
                        principalColumn: "GuildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserZhouVariants",
                columns: table => new
                {
                    UserZhouVariantID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Borrow = table.Column<int>(type: "INTEGER", nullable: true),
                    UserID = table.Column<string>(type: "TEXT", nullable: true),
                    ZhouVariantID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserZhouVariants", x => x.UserZhouVariantID);
                    table.ForeignKey(
                        name: "FK_UserZhouVariants_AspNetUsers_UserID",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserZhouVariants_ZhouVariants_ZhouVariantID",
                        column: x => x.ZhouVariantID,
                        principalTable: "ZhouVariants",
                        principalColumn: "ZhouVariantID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCombos",
                columns: table => new
                {
                    UserComboID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BorrowInfo = table.Column<string>(type: "TEXT", nullable: true),
                    Boss1 = table.Column<int>(type: "INTEGER", nullable: false),
                    Boss2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Boss3 = table.Column<int>(type: "INTEGER", nullable: false),
                    Damage1 = table.Column<int>(type: "INTEGER", nullable: false),
                    Damage2 = table.Column<int>(type: "INTEGER", nullable: false),
                    Damage3 = table.Column<int>(type: "INTEGER", nullable: false),
                    GuildID = table.Column<int>(type: "INTEGER", nullable: false),
                    NetValue = table.Column<float>(type: "REAL", nullable: false),
                    SelectedZhou = table.Column<int>(type: "INTEGER", nullable: true),
                    UserID = table.Column<string>(type: "TEXT", nullable: true),
                    Value = table.Column<float>(type: "REAL", nullable: false),
                    Zhou1ID = table.Column<int>(type: "INTEGER", nullable: true),
                    Zhou2ID = table.Column<int>(type: "INTEGER", nullable: true),
                    Zhou3ID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCombos", x => x.UserComboID);
                    table.ForeignKey(
                        name: "FK_UserCombos_AspNetUsers_UserID",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserCombos_Guilds_GuildID",
                        column: x => x.GuildID,
                        principalTable: "Guilds",
                        principalColumn: "GuildID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCombos_UserZhouVariants_Zhou1ID",
                        column: x => x.Zhou1ID,
                        principalTable: "UserZhouVariants",
                        principalColumn: "UserZhouVariantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserCombos_UserZhouVariants_Zhou2ID",
                        column: x => x.Zhou2ID,
                        principalTable: "UserZhouVariants",
                        principalColumn: "UserZhouVariantID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserCombos_UserZhouVariants_Zhou3ID",
                        column: x => x.Zhou3ID,
                        principalTable: "UserZhouVariants",
                        principalColumn: "UserZhouVariantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildBossStatuses_BossID",
                table: "GuildBossStatuses",
                column: "BossID");

            migrationBuilder.CreateIndex(
                name: "IX_GuildBossStatuses_GuildID",
                table: "GuildBossStatuses",
                column: "GuildID");

            migrationBuilder.CreateIndex(
                name: "IX_UserCombos_GuildID",
                table: "UserCombos",
                column: "GuildID");

            migrationBuilder.CreateIndex(
                name: "IX_UserCombos_UserID",
                table: "UserCombos",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserCombos_Zhou1ID",
                table: "UserCombos",
                column: "Zhou1ID");

            migrationBuilder.CreateIndex(
                name: "IX_UserCombos_Zhou2ID",
                table: "UserCombos",
                column: "Zhou2ID");

            migrationBuilder.CreateIndex(
                name: "IX_UserCombos_Zhou3ID",
                table: "UserCombos",
                column: "Zhou3ID");

            migrationBuilder.CreateIndex(
                name: "IX_UserZhouVariants_UserID",
                table: "UserZhouVariants",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserZhouVariants_ZhouVariantID",
                table: "UserZhouVariants",
                column: "ZhouVariantID");
        }
    }
}
