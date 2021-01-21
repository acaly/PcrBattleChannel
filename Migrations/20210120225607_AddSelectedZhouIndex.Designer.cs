﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PcrBattleChannel.Data;

namespace PcrBattleChannel.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20210120225607_AddSelectedZhouIndex")]
    partial class AddSelectedZhouIndex
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.2");

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex")
                        .HasFilter("[NormalizedName] IS NOT NULL");

                    b.ToTable("AspNetRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<string>("ClaimType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("ProviderKey")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("RoleId")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("LoginProvider")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Name")
                        .HasMaxLength(128)
                        .HasColumnType("nvarchar(128)");

                    b.Property<string>("Value")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.BattleStage", b =>
                {
                    b.Property<int>("BattleStageID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<int>("GlobalDataID")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Order")
                        .HasColumnType("int");

                    b.Property<string>("ShortName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("StartLap")
                        .HasColumnType("int");

                    b.HasKey("BattleStageID");

                    b.HasIndex("GlobalDataID");

                    b.ToTable("BattleStages");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.Boss", b =>
                {
                    b.Property<int>("BossID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<int>("BattleStageID")
                        .HasColumnType("int");

                    b.Property<int>("Life")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<float>("Score")
                        .HasColumnType("real");

                    b.Property<string>("ShortName")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("BossID");

                    b.HasIndex("BattleStageID");

                    b.ToTable("Bosses");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.Character", b =>
                {
                    b.Property<int>("CharacterID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<bool>("HasWeapon")
                        .HasColumnType("bit");

                    b.Property<int>("InternalID")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<float>("Range")
                        .HasColumnType("real");

                    b.Property<int>("Rarity")
                        .HasColumnType("int");

                    b.HasKey("CharacterID");

                    b.ToTable("Characters");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.CharacterConfig", b =>
                {
                    b.Property<int>("CharacterConfigID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<int>("CharacterID")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("GuildID")
                        .HasColumnType("int");

                    b.Property<int>("Kind")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("CharacterConfigID");

                    b.HasIndex("CharacterID");

                    b.HasIndex("GuildID");

                    b.ToTable("CharacterConfigs");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.GlobalData", b =>
                {
                    b.Property<int>("GlobalDataID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<DateTime>("EndTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("SeasonName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("datetime2");

                    b.HasKey("GlobalDataID");

                    b.ToTable("GlobalData");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.Guild", b =>
                {
                    b.Property<int>("GuildID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<float>("BossDamageRatio")
                        .HasColumnType("real");

                    b.Property<int>("BossIndex")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("LastCalculation")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OwnerID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<float>("PredictBossDamageRatio")
                        .HasColumnType("real");

                    b.Property<int>("PredictBossIndex")
                        .HasColumnType("int");

                    b.HasKey("GuildID");

                    b.HasIndex("OwnerID");

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.GuildBossStatus", b =>
                {
                    b.Property<int>("GuildBossStatusID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<int>("BossID")
                        .HasColumnType("int");

                    b.Property<float>("DamageRatio")
                        .HasColumnType("real");

                    b.Property<int>("DisplayColumn")
                        .HasColumnType("int");

                    b.Property<int>("DisplayRow")
                        .HasColumnType("int");

                    b.Property<int>("GuildID")
                        .HasColumnType("int");

                    b.Property<bool>("IsPlan")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("GuildBossStatusID");

                    b.HasIndex("BossID");

                    b.HasIndex("GuildID");

                    b.ToTable("GuildBossStatuses");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.PcrIdentityUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("int");

                    b.Property<int>("Attempts")
                        .HasColumnType("int");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("bit");

                    b.Property<string>("GameID")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("GuildID")
                        .HasColumnType("int");

                    b.Property<bool>("IsGuildAdmin")
                        .HasColumnType("bit");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("NextAttemptPlanIndex")
                        .HasColumnType("int");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("bit");

                    b.Property<decimal>("QQID")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("bit");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("nvarchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("GuildID");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.UserCharacterConfig", b =>
                {
                    b.Property<int>("UserCharacterConfigID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<int>("CharacterConfigID")
                        .HasColumnType("int");

                    b.Property<string>("UserID")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("UserCharacterConfigID");

                    b.HasIndex("CharacterConfigID");

                    b.HasIndex("UserID");

                    b.ToTable("UserCharacterConfigs");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.UserCharacterStatus", b =>
                {
                    b.Property<int>("UserCharacterStatusID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<int>("CharacterID")
                        .HasColumnType("int");

                    b.Property<bool>("IsUsed")
                        .HasColumnType("bit");

                    b.Property<string>("UserID")
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("UserCharacterStatusID");

                    b.HasIndex("CharacterID");

                    b.HasIndex("UserID");

                    b.ToTable("UserCharacterStatuses");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.UserCombo", b =>
                {
                    b.Property<int>("UserComboID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<int>("GuildID")
                        .HasColumnType("int");

                    b.Property<float>("NetValue")
                        .HasColumnType("real");

                    b.Property<int?>("SelectedZhou")
                        .HasColumnType("int");

                    b.Property<string>("UserID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<float>("Value")
                        .HasColumnType("real");

                    b.Property<int?>("Zhou1ID")
                        .HasColumnType("int");

                    b.Property<int?>("Zhou2ID")
                        .HasColumnType("int");

                    b.Property<int?>("Zhou3ID")
                        .HasColumnType("int");

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
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<int?>("Borrow")
                        .HasColumnType("int");

                    b.Property<string>("UserID")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("ZhouVariantID")
                        .HasColumnType("int");

                    b.HasKey("UserZhouVariantID");

                    b.HasIndex("UserID");

                    b.HasIndex("ZhouVariantID");

                    b.ToTable("UserZhouVariants");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.Zhou", b =>
                {
                    b.Property<int>("ZhouID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<int>("BossID")
                        .HasColumnType("int");

                    b.Property<int?>("C1ID")
                        .HasColumnType("int");

                    b.Property<int?>("C2ID")
                        .HasColumnType("int");

                    b.Property<int?>("C3ID")
                        .HasColumnType("int");

                    b.Property<int?>("C4ID")
                        .HasColumnType("int");

                    b.Property<int?>("C5ID")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("GuildID")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

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
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<string>("Content")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Damage")
                        .HasColumnType("int");

                    b.Property<bool>("IsDraft")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("ZhouID")
                        .HasColumnType("int");

                    b.HasKey("ZhouVariantID");

                    b.HasIndex("ZhouID");

                    b.ToTable("ZhouVariants");
                });

            modelBuilder.Entity("PcrBattleChannel.Models.ZhouVariantCharacterConfig", b =>
                {
                    b.Property<int>("ZhouVariantCharacterConfigID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<int?>("CharacterConfigID")
                        .HasColumnType("int");

                    b.Property<int>("CharacterIndex")
                        .HasColumnType("int");

                    b.Property<int>("OrGroupIndex")
                        .HasColumnType("int");

                    b.Property<int>("ZhouVariantID")
                        .HasColumnType("int");

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
                    b.HasOne("PcrBattleChannel.Models.Guild", "Guild")
                        .WithMany("Members")
                        .HasForeignKey("GuildID");

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
                        .HasForeignKey("Zhou1ID");

                    b.HasOne("PcrBattleChannel.Models.UserZhouVariant", "Zhou2")
                        .WithMany()
                        .HasForeignKey("Zhou2ID");

                    b.HasOne("PcrBattleChannel.Models.UserZhouVariant", "Zhou3")
                        .WithMany()
                        .HasForeignKey("Zhou3ID");

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
