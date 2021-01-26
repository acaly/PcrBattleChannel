﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PcrBattleChannel.Data;

namespace PcrBattleChannel.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.2");

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("AspNetRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ClaimType")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("TEXT");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("ClaimType")
                        .HasColumnType("TEXT");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<string>("ProviderKey")
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<string>("RoleId")
                        .HasColumnType("TEXT");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasMaxLength(128)
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.BattleStage", b =>
                {
                    b.Property<int>("BattleStageID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("GlobalDataID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<int>("Order")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ShortName")
                        .HasColumnType("TEXT");

                    b.Property<int>("StartLap")
                        .HasColumnType("INTEGER");

                    b.HasKey("BattleStageID");

                    b.HasIndex("GlobalDataID");

                    b.ToTable("BattleStages");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.Boss", b =>
                {
                    b.Property<int>("BossID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("BattleStageID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Life")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<float>("Score")
                        .HasColumnType("REAL");

                    b.Property<string>("ShortName")
                        .HasColumnType("TEXT");

                    b.HasKey("BossID");

                    b.HasIndex("BattleStageID");

                    b.ToTable("Bosses");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.Character", b =>
                {
                    b.Property<int>("CharacterID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("HasWeapon")
                        .HasColumnType("INTEGER");

                    b.Property<int>("InternalID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<float>("Range")
                        .HasColumnType("REAL");

                    b.Property<int>("Rarity")
                        .HasColumnType("INTEGER");

                    b.HasKey("CharacterID");

                    b.ToTable("Characters");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.CharacterConfig", b =>
                {
                    b.Property<int>("CharacterConfigID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CharacterID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<int>("GuildID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Kind")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("CharacterConfigID");

                    b.HasIndex("CharacterID");

                    b.HasIndex("GuildID");

                    b.ToTable("CharacterConfigs");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.GlobalData", b =>
                {
                    b.Property<int>("GlobalDataID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("EndTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("SeasonName")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("TEXT");

                    b.HasKey("GlobalDataID");

                    b.ToTable("GlobalData");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.Guild", b =>
                {
                    b.Property<int>("GuildID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<float>("BossDamageRatio")
                        .HasColumnType("REAL");

                    b.Property<int>("BossIndex")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastCalculation")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastYobotSync")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastZhouUpdate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("OwnerID")
                        .HasColumnType("TEXT");

                    b.Property<float>("PredictBossDamageRatio")
                        .HasColumnType("REAL");

                    b.Property<int>("PredictBossIndex")
                        .HasColumnType("INTEGER");

                    b.Property<string>("YobotAPI")
                        .HasColumnType("TEXT");

                    b.HasKey("GuildID");

                    b.HasIndex("OwnerID");

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.GuildBossStatus", b =>
                {
                    b.Property<int>("GuildBossStatusID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("BossID")
                        .HasColumnType("INTEGER");

                    b.Property<float>("DamageRatio")
                        .HasColumnType("REAL");

                    b.Property<int>("DisplayColumn")
                        .HasColumnType("INTEGER");

                    b.Property<int>("DisplayRow")
                        .HasColumnType("INTEGER");

                    b.Property<int>("GuildID")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsPlan")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("GuildBossStatusID");

                    b.HasIndex("BossID");

                    b.HasIndex("GuildID");

                    b.ToTable("GuildBossStatuses");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.PcrIdentityUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("Attempt1Borrow")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("Attempt1ID")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("Attempt2Borrow")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("Attempt2ID")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("Attempt3Borrow")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("Attempt3ID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Attempts")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("TEXT");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("INTEGER");

                    b.Property<string>("GameID")
                        .HasColumnType("TEXT");

                    b.Property<int>("GuessedAttempts")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("GuildID")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsGuildAdmin")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsIgnored")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("LastComboUpdate")
                        .HasColumnType("TEXT");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("TEXT");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("TEXT");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("QQID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("TEXT");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Attempt1ID");

                    b.HasIndex("Attempt2ID");

                    b.HasIndex("Attempt3ID");

                    b.HasIndex("GuildID");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.UserCharacterConfig", b =>
                {
                    b.Property<int>("UserCharacterConfigID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CharacterConfigID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UserID")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("UserCharacterConfigID");

                    b.HasIndex("CharacterConfigID");

                    b.HasIndex("UserID");

                    b.ToTable("UserCharacterConfigs");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.UserCharacterStatus", b =>
                {
                    b.Property<int>("UserCharacterStatusID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("CharacterID")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsUsed")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UserID")
                        .HasColumnType("TEXT");

                    b.HasKey("UserCharacterStatusID");

                    b.HasIndex("CharacterID");

                    b.HasIndex("UserID");

                    b.ToTable("UserCharacterStatuses");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.UserCombo", b =>
                {
                    b.Property<int>("UserComboID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("BorrowInfo")
                        .HasColumnType("TEXT");

                    b.Property<int>("GuildID")
                        .HasColumnType("INTEGER");

                    b.Property<float>("NetValue")
                        .HasColumnType("REAL");

                    b.Property<int?>("SelectedZhou")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UserID")
                        .HasColumnType("TEXT");

                    b.Property<float>("Value")
                        .HasColumnType("REAL");

                    b.Property<int?>("Zhou1ID")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("Zhou2ID")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("Zhou3ID")
                        .HasColumnType("INTEGER");

                    b.HasKey("UserComboID");

                    b.HasIndex("GuildID");

                    b.HasIndex("UserID");

                    b.HasIndex("Zhou1ID");

                    b.HasIndex("Zhou2ID");

                    b.HasIndex("Zhou3ID");

                    b.ToTable("UserCombos");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.UserZhouVariant", b =>
                {
                    b.Property<int>("UserZhouVariantID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("Borrow")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UserID")
                        .HasColumnType("TEXT");

                    b.Property<int>("ZhouVariantID")
                        .HasColumnType("INTEGER");

                    b.HasKey("UserZhouVariantID");

                    b.HasIndex("UserID");

                    b.HasIndex("ZhouVariantID");

                    b.ToTable("UserZhouVariants");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.Zhou", b =>
                {
                    b.Property<int>("ZhouID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("BossID")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("C1ID")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("C2ID")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("C3ID")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("C4ID")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("C5ID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .HasColumnType("TEXT");

                    b.Property<int>("GuildID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("ZhouID");

                    b.HasIndex("BossID");

                    b.HasIndex("C1ID");

                    b.HasIndex("C2ID");

                    b.HasIndex("C3ID");

                    b.HasIndex("C4ID");

                    b.HasIndex("C5ID");

                    b.HasIndex("GuildID");

                    b.ToTable("Zhous");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.ZhouVariant", b =>
                {
                    b.Property<int>("ZhouVariantID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Content")
                        .HasColumnType("TEXT");

                    b.Property<int>("Damage")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsDraft")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<int>("ZhouID")
                        .HasColumnType("INTEGER");

                    b.HasKey("ZhouVariantID");

                    b.HasIndex("ZhouID");

                    b.ToTable("ZhouVariants");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.ZhouVariantCharacterConfig", b =>
                {
                    b.Property<int>("ZhouVariantCharacterConfigID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("CharacterConfigID")
                        .HasColumnType("INTEGER");

                    b.Property<int>("CharacterIndex")
                        .HasColumnType("INTEGER");

                    b.Property<int>("OrGroupIndex")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ZhouVariantID")
                        .HasColumnType("INTEGER");

                    b.HasKey("ZhouVariantCharacterConfigID");

                    b.HasIndex("CharacterConfigID");

                    b.HasIndex("ZhouVariantID");

                    b.ToTable("ZhouVariantCharacterConfigs");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.PcrIdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.PcrIdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PcrBattleChannel.Models.PcrIdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.PcrIdentityUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("PcrBattleChannel.Models.BattleStage", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.GlobalData", "GlobalData")
                        .WithMany()
                        .HasForeignKey("GlobalDataID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GlobalData");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.Boss", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.BattleStage", "BattleStage")
                        .WithMany()
                        .HasForeignKey("BattleStageID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BattleStage");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.CharacterConfig", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.Character", "Character")
                        .WithMany()
                        .HasForeignKey("CharacterID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PcrBattleChannel.Models.Guild", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Character");

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.Guild", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.PcrIdentityUser", "Owner")
                        .WithMany()
                        .HasForeignKey("OwnerID");

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.GuildBossStatus", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.Boss", "Boss")
                        .WithMany()
                        .HasForeignKey("BossID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PcrBattleChannel.Models.Guild", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Boss");

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.PcrIdentityUser", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.UserZhouVariant", "Attempt1")
                        .WithMany()
                        .HasForeignKey("Attempt1ID");

                    b.HasOne("PcrBattleChannel.Models.UserZhouVariant", "Attempt2")
                        .WithMany()
                        .HasForeignKey("Attempt2ID");

                    b.HasOne("PcrBattleChannel.Models.UserZhouVariant", "Attempt3")
                        .WithMany()
                        .HasForeignKey("Attempt3ID");

                    b.HasOne("PcrBattleChannel.Models.Guild", "Guild")
                        .WithMany("Members")
                        .HasForeignKey("GuildID");

                    b.Navigation("Attempt1");

                    b.Navigation("Attempt2");

                    b.Navigation("Attempt3");

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.UserCharacterConfig", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.CharacterConfig", "CharacterConfig")
                        .WithMany()
                        .HasForeignKey("CharacterConfigID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PcrBattleChannel.Models.PcrIdentityUser", "User")
                        .WithMany()
                        .HasForeignKey("UserID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CharacterConfig");

                    b.Navigation("User");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.UserCharacterStatus", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.Character", "Character")
                        .WithMany()
                        .HasForeignKey("CharacterID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PcrBattleChannel.Models.PcrIdentityUser", "User")
                        .WithMany("CharacterStatuses")
                        .HasForeignKey("UserID");

                    b.Navigation("Character");

                    b.Navigation("User");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.UserCombo", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.Guild", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PcrBattleChannel.Models.PcrIdentityUser", "User")
                        .WithMany("Combos")
                        .HasForeignKey("UserID");

                    b.HasOne("PcrBattleChannel.Models.UserZhouVariant", "Zhou1")
                        .WithMany()
                        .HasForeignKey("Zhou1ID")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("PcrBattleChannel.Models.UserZhouVariant", "Zhou2")
                        .WithMany()
                        .HasForeignKey("Zhou2ID")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("PcrBattleChannel.Models.UserZhouVariant", "Zhou3")
                        .WithMany()
                        .HasForeignKey("Zhou3ID")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.Navigation("Guild");

                    b.Navigation("User");

                    b.Navigation("Zhou1");

                    b.Navigation("Zhou2");

                    b.Navigation("Zhou3");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.UserZhouVariant", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.PcrIdentityUser", "User")
                        .WithMany("ZhouVariants")
                        .HasForeignKey("UserID");

                    b.HasOne("PcrBattleChannel.Models.ZhouVariant", "ZhouVariant")
                        .WithMany()
                        .HasForeignKey("ZhouVariantID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");

                    b.Navigation("ZhouVariant");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.Zhou", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.Boss", "Boss")
                        .WithMany()
                        .HasForeignKey("BossID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PcrBattleChannel.Models.Character", "C1")
                        .WithMany()
                        .HasForeignKey("C1ID");

                    b.HasOne("PcrBattleChannel.Models.Character", "C2")
                        .WithMany()
                        .HasForeignKey("C2ID");

                    b.HasOne("PcrBattleChannel.Models.Character", "C3")
                        .WithMany()
                        .HasForeignKey("C3ID");

                    b.HasOne("PcrBattleChannel.Models.Character", "C4")
                        .WithMany()
                        .HasForeignKey("C4ID");

                    b.HasOne("PcrBattleChannel.Models.Character", "C5")
                        .WithMany()
                        .HasForeignKey("C5ID");

                    b.HasOne("PcrBattleChannel.Models.Guild", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Boss");

                    b.Navigation("C1");

                    b.Navigation("C2");

                    b.Navigation("C3");

                    b.Navigation("C4");

                    b.Navigation("C5");

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.ZhouVariant", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.Zhou", "Zhou")
                        .WithMany("Variants")
                        .HasForeignKey("ZhouID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Zhou");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.ZhouVariantCharacterConfig", b =>
                {
                    b.HasOne("PcrBattleChannel.Models.CharacterConfig", "CharacterConfig")
                        .WithMany()
                        .HasForeignKey("CharacterConfigID");

                    b.HasOne("PcrBattleChannel.Models.ZhouVariant", "ZhouVariant")
                        .WithMany("CharacterConfigs")
                        .HasForeignKey("ZhouVariantID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CharacterConfig");

                    b.Navigation("ZhouVariant");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.Guild", b =>
                {
                    b.Navigation("Members");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.PcrIdentityUser", b =>
                {
                    b.Navigation("CharacterStatuses");

                    b.Navigation("Combos");

                    b.Navigation("ZhouVariants");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.Zhou", b =>
                {
                    b.Navigation("Variants");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.ZhouVariant", b =>
                {
                    b.Navigation("CharacterConfigs");
                });
#pragma warning restore 612, 618
        }
    }
}
