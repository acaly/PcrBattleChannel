﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace PcrBattleChannel.Data
{
    public class ApplicationDbContext : IdentityDbContext<PcrIdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //TODO is this one necessary?
            builder.Entity<Guild>()
                .HasMany(g => g.Members)
                .WithOne(m => m.Guild)
                .HasForeignKey(m => m.GuildID);

            builder.Entity<ZhouVariantCharacterConfig>()
                .HasOne(c => c.ZhouVariant)
                .WithMany(v => v.CharacterConfigs)
                .HasForeignKey(c => c.ZhouVariantID)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PcrIdentityUser>()
                .HasOne(u => u.Attempt1)
                .WithMany()
                .HasForeignKey(u => u.Attempt1ID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.Entity<PcrIdentityUser>()
                .HasOne(u => u.Attempt2)
                .WithMany()
                .HasForeignKey(u => u.Attempt2ID)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.Entity<PcrIdentityUser>()
                .HasOne(u => u.Attempt3)
                .WithMany()
                .HasForeignKey(u => u.Attempt3ID)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }

        //Game characters. Imported from Admin/EditGlobalData.
        public DbSet<Character> Characters { get; set; }

        //Tables associated with a guild/user.
        public DbSet<Guild> Guilds { get; set; }
        public DbSet<CharacterConfig> CharacterConfigs { get; set; }
        public DbSet<UserCharacterStatus> UserCharacterStatuses { get; set; }
        public DbSet<UserCharacterConfig> UserCharacterConfigs { get; set; }

        //Tables associated with a session.
        public DbSet<GlobalData> GlobalData { get; set; }
        public DbSet<BattleStage> BattleStages { get; set; }
        public DbSet<Boss> Bosses { get; set; }

        //Tables associated with a session and a guild/user.
        public DbSet<Zhou> Zhous { get; set; }
        public DbSet<ZhouVariant> ZhouVariants { get; set; }
        public DbSet<ZhouVariantCharacterConfig> ZhouVariantCharacterConfigs { get; set; }
    }
}
