using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PcrBattleChannel.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    CharacterID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InternalID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rarity = table.Column<int>(type: "int", nullable: false),
                    HasWeapon = table.Column<bool>(type: "bit", nullable: false),
                    Range = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.CharacterID);
                });

            migrationBuilder.CreateTable(
                name: "GlobalData",
                columns: table => new
                {
                    GlobalDataID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SeasonName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalData", x => x.GlobalDataID);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BattleStages",
                columns: table => new
                {
                    BattleStageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GlobalDataID = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartLap = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleStages", x => x.BattleStageID);
                    table.ForeignKey(
                        name: "FK_BattleStages_GlobalData_GlobalDataID",
                        column: x => x.GlobalDataID,
                        principalTable: "GlobalData",
                        principalColumn: "GlobalDataID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bosses",
                columns: table => new
                {
                    BossID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BattleStageID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Life = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bosses", x => x.BossID);
                    table.ForeignKey(
                        name: "FK_Bosses_BattleStages_BattleStageID",
                        column: x => x.BattleStageID,
                        principalTable: "BattleStages",
                        principalColumn: "BattleStageID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    GuildID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    BossIndex = table.Column<int>(type: "int", nullable: false),
                    BossDamageRatio = table.Column<float>(type: "real", nullable: false),
                    PredictBossIndex = table.Column<int>(type: "int", nullable: false),
                    PredictBossDamageRatio = table.Column<float>(type: "real", nullable: false),
                    LastCalculation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.GuildID);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GameID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QQID = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    GuildID = table.Column<int>(type: "int", nullable: true),
                    IsGuildAdmin = table.Column<bool>(type: "bit", nullable: false),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    NextAttemptPlanIndex = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Guilds_GuildID",
                        column: x => x.GuildID,
                        principalTable: "Guilds",
                        principalColumn: "GuildID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CharacterConfigs",
                columns: table => new
                {
                    CharacterConfigID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GuildID = table.Column<int>(type: "int", nullable: false),
                    CharacterID = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterConfigs", x => x.CharacterConfigID);
                    table.ForeignKey(
                        name: "FK_CharacterConfigs_Characters_CharacterID",
                        column: x => x.CharacterID,
                        principalTable: "Characters",
                        principalColumn: "CharacterID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterConfigs_Guilds_GuildID",
                        column: x => x.GuildID,
                        principalTable: "Guilds",
                        principalColumn: "GuildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildBossStatuses",
                columns: table => new
                {
                    GuildBossStatusID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsPlan = table.Column<bool>(type: "bit", nullable: false),
                    GuildID = table.Column<int>(type: "int", nullable: false),
                    BossID = table.Column<int>(type: "int", nullable: false),
                    DisplayRow = table.Column<int>(type: "int", nullable: false),
                    DisplayColumn = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DamageRatio = table.Column<float>(type: "real", nullable: false)
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
                name: "Zhous",
                columns: table => new
                {
                    ZhouID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GuildID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BossID = table.Column<int>(type: "int", nullable: false),
                    C1ID = table.Column<int>(type: "int", nullable: true),
                    C2ID = table.Column<int>(type: "int", nullable: true),
                    C3ID = table.Column<int>(type: "int", nullable: true),
                    C4ID = table.Column<int>(type: "int", nullable: true),
                    C5ID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zhous", x => x.ZhouID);
                    table.ForeignKey(
                        name: "FK_Zhous_Bosses_BossID",
                        column: x => x.BossID,
                        principalTable: "Bosses",
                        principalColumn: "BossID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Zhous_Characters_C1ID",
                        column: x => x.C1ID,
                        principalTable: "Characters",
                        principalColumn: "CharacterID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Zhous_Characters_C2ID",
                        column: x => x.C2ID,
                        principalTable: "Characters",
                        principalColumn: "CharacterID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Zhous_Characters_C3ID",
                        column: x => x.C3ID,
                        principalTable: "Characters",
                        principalColumn: "CharacterID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Zhous_Characters_C4ID",
                        column: x => x.C4ID,
                        principalTable: "Characters",
                        principalColumn: "CharacterID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Zhous_Characters_C5ID",
                        column: x => x.C5ID,
                        principalTable: "Characters",
                        principalColumn: "CharacterID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Zhous_Guilds_GuildID",
                        column: x => x.GuildID,
                        principalTable: "Guilds",
                        principalColumn: "GuildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCharacterStatuses",
                columns: table => new
                {
                    UserCharacterStatusID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CharacterID = table.Column<int>(type: "int", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCharacterStatuses", x => x.UserCharacterStatusID);
                    table.ForeignKey(
                        name: "FK_UserCharacterStatuses_AspNetUsers_UserID",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserCharacterStatuses_Characters_CharacterID",
                        column: x => x.CharacterID,
                        principalTable: "Characters",
                        principalColumn: "CharacterID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCharacterConfigs",
                columns: table => new
                {
                    UserCharacterConfigID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CharacterConfigID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCharacterConfigs", x => x.UserCharacterConfigID);
                    table.ForeignKey(
                        name: "FK_UserCharacterConfigs_AspNetUsers_UserID",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCharacterConfigs_CharacterConfigs_CharacterConfigID",
                        column: x => x.CharacterConfigID,
                        principalTable: "CharacterConfigs",
                        principalColumn: "CharacterConfigID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ZhouVariants",
                columns: table => new
                {
                    ZhouVariantID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZhouID = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDraft = table.Column<bool>(type: "bit", nullable: false),
                    Damage = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZhouVariants", x => x.ZhouVariantID);
                    table.ForeignKey(
                        name: "FK_ZhouVariants_Zhous_ZhouID",
                        column: x => x.ZhouID,
                        principalTable: "Zhous",
                        principalColumn: "ZhouID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserZhouVariants",
                columns: table => new
                {
                    UserZhouVariantID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ZhouVariantID = table.Column<int>(type: "int", nullable: false)
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
                name: "ZhouVariantCharacterConfigs",
                columns: table => new
                {
                    ZhouVariantCharacterConfigID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ZhouVariantID = table.Column<int>(type: "int", nullable: false),
                    CharacterConfigID = table.Column<int>(type: "int", nullable: true),
                    CharacterIndex = table.Column<int>(type: "int", nullable: false),
                    OrGroupIndex = table.Column<int>(type: "int", nullable: false),
                    AllowBorrow = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZhouVariantCharacterConfigs", x => x.ZhouVariantCharacterConfigID);
                    table.ForeignKey(
                        name: "FK_ZhouVariantCharacterConfigs_CharacterConfigs_CharacterConfigID",
                        column: x => x.CharacterConfigID,
                        principalTable: "CharacterConfigs",
                        principalColumn: "CharacterConfigID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ZhouVariantCharacterConfigs_ZhouVariants_ZhouVariantID",
                        column: x => x.ZhouVariantID,
                        principalTable: "ZhouVariants",
                        principalColumn: "ZhouVariantID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCombos",
                columns: table => new
                {
                    UserComboID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsSelected = table.Column<bool>(type: "bit", nullable: false),
                    NetValue = table.Column<float>(type: "real", nullable: false),
                    Value = table.Column<float>(type: "real", nullable: false),
                    UserID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    GuildID = table.Column<int>(type: "int", nullable: false),
                    Zhou1ID = table.Column<int>(type: "int", nullable: true),
                    Zhou1Borrow = table.Column<int>(type: "int", nullable: true),
                    Zhou2ID = table.Column<int>(type: "int", nullable: true),
                    Zhou2Borrow = table.Column<int>(type: "int", nullable: true),
                    Zhou3ID = table.Column<int>(type: "int", nullable: true),
                    Zhou3Borrow = table.Column<int>(type: "int", nullable: true)
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
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GuildID",
                table: "AspNetUsers",
                column: "GuildID");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BattleStages_GlobalDataID",
                table: "BattleStages",
                column: "GlobalDataID");

            migrationBuilder.CreateIndex(
                name: "IX_Bosses_BattleStageID",
                table: "Bosses",
                column: "BattleStageID");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterConfigs_CharacterID",
                table: "CharacterConfigs",
                column: "CharacterID");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterConfigs_GuildID",
                table: "CharacterConfigs",
                column: "GuildID");

            migrationBuilder.CreateIndex(
                name: "IX_GuildBossStatuses_BossID",
                table: "GuildBossStatuses",
                column: "BossID");

            migrationBuilder.CreateIndex(
                name: "IX_GuildBossStatuses_GuildID",
                table: "GuildBossStatuses",
                column: "GuildID");

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_OwnerID",
                table: "Guilds",
                column: "OwnerID");

            migrationBuilder.CreateIndex(
                name: "IX_UserCharacterConfigs_CharacterConfigID",
                table: "UserCharacterConfigs",
                column: "CharacterConfigID");

            migrationBuilder.CreateIndex(
                name: "IX_UserCharacterConfigs_UserID",
                table: "UserCharacterConfigs",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserCharacterStatuses_CharacterID",
                table: "UserCharacterStatuses",
                column: "CharacterID");

            migrationBuilder.CreateIndex(
                name: "IX_UserCharacterStatuses_UserID",
                table: "UserCharacterStatuses",
                column: "UserID");

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

            migrationBuilder.CreateIndex(
                name: "IX_Zhous_BossID",
                table: "Zhous",
                column: "BossID");

            migrationBuilder.CreateIndex(
                name: "IX_Zhous_C1ID",
                table: "Zhous",
                column: "C1ID");

            migrationBuilder.CreateIndex(
                name: "IX_Zhous_C2ID",
                table: "Zhous",
                column: "C2ID");

            migrationBuilder.CreateIndex(
                name: "IX_Zhous_C3ID",
                table: "Zhous",
                column: "C3ID");

            migrationBuilder.CreateIndex(
                name: "IX_Zhous_C4ID",
                table: "Zhous",
                column: "C4ID");

            migrationBuilder.CreateIndex(
                name: "IX_Zhous_C5ID",
                table: "Zhous",
                column: "C5ID");

            migrationBuilder.CreateIndex(
                name: "IX_Zhous_GuildID",
                table: "Zhous",
                column: "GuildID");

            migrationBuilder.CreateIndex(
                name: "IX_ZhouVariantCharacterConfigs_CharacterConfigID",
                table: "ZhouVariantCharacterConfigs",
                column: "CharacterConfigID");

            migrationBuilder.CreateIndex(
                name: "IX_ZhouVariantCharacterConfigs_ZhouVariantID",
                table: "ZhouVariantCharacterConfigs",
                column: "ZhouVariantID");

            migrationBuilder.CreateIndex(
                name: "IX_ZhouVariants_ZhouID",
                table: "ZhouVariants",
                column: "ZhouID");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                table: "AspNetUserRoles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                table: "AspNetUserClaims",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                table: "AspNetUserLogins",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                table: "AspNetUserTokens",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Guilds_AspNetUsers_OwnerID",
                table: "Guilds",
                column: "OwnerID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guilds_AspNetUsers_OwnerID",
                table: "Guilds");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "GuildBossStatuses");

            migrationBuilder.DropTable(
                name: "UserCharacterConfigs");

            migrationBuilder.DropTable(
                name: "UserCharacterStatuses");

            migrationBuilder.DropTable(
                name: "UserCombos");

            migrationBuilder.DropTable(
                name: "ZhouVariantCharacterConfigs");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "UserZhouVariants");

            migrationBuilder.DropTable(
                name: "CharacterConfigs");

            migrationBuilder.DropTable(
                name: "ZhouVariants");

            migrationBuilder.DropTable(
                name: "Zhous");

            migrationBuilder.DropTable(
                name: "Bosses");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "BattleStages");

            migrationBuilder.DropTable(
                name: "GlobalData");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Guilds");
        }
    }
}
