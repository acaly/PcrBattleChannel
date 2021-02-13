using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Home
{
    public class IndexModel : PageModel
    {
        private readonly InMemoryStorageContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public IndexModel(InMemoryStorageContext memContext, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = memContext;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public Guild Guild { get; private set; }
        public bool IsAdmin { get; private set; }
        public int GuildPredictBossIndex { get; private set; }
        public float GuildPredictBossRatio { get; private set; }

        public int[] FirstLapForStages { get; private set; }
        public List<List<string>> BossNames { get; private set; }
        public List<List<string>> BossShortNames { get; private set; }
        public int Attempts { get; private set; }
        public int GuessedAttempts { get; private set; }
        public int RemainingAttempts { get; private set; }
        public int UnknownAttempts { get; private set; }
        public List<(Boss boss, float value)> BossStatus { get; private set; }

        public string GetBossName(int stage, int boss)
        {
            try
            {
                return BossNames[stage][boss];
            }
            catch
            {
                return string.Empty;
            }
        }

        public string GetBossShortName(int stage, int boss)
        {
            try
            {
                return BossShortNames[stage][boss];
            }
            catch
            {
                return string.Empty;
            }
        }

        public (int stage, int lap, int boss) ConvLap(int bossIndex)
        {
            int stage = 0, lap = 0;
            while (bossIndex >= BossNames[stage].Count)
            {
                bossIndex -= BossNames[stage].Count;
                lap += 1;
                if (stage + 1 == FirstLapForStages.Length)
                {
                    if (BossNames[stage].Count == 0)
                    {
                        throw new Exception("Invalid boss data");
                    }
                }
                else if (lap >= FirstLapForStages[stage + 1])
                {
                    stage += 1;
                }
            }
            return (stage, lap, bossIndex);
        }

        public async Task<IActionResult> OnGet()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return Page();
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue)
            {
                return Page();
            }
            IsAdmin = user.IsGuildAdmin;

            Guild = await _context.DbContext.Guilds.FirstOrDefaultAsync(g => g.GuildID == user.GuildID.Value);
            var imGuild = await _context.GetGuildAsync(user.GuildID.Value);
            GuildPredictBossIndex = imGuild.PredictBossIndex;
            GuildPredictBossRatio = imGuild.PredictBossDamageRatio;

            var stages = await _context.DbContext.BattleStages.OrderBy(s => s.StartLap).ToListAsync();
            FirstLapForStages = stages.Select(s => s.StartLap).OrderBy(i => i).ToArray();

            BossNames = new();
            BossShortNames = new();
            var allBosses = await _context.DbContext.Bosses
                .ToListAsync();
            foreach (var s in stages)
            {
                var b = allBosses
                    .Where(b => b.BattleStageID == s.BattleStageID)
                    .OrderBy(b => b.BossID);
                BossNames.Add(b.Select(b => b.Name).ToList());
                BossShortNames.Add(b.Select(b => b.ShortName).ToList());
            }

            var usedAttempts = await _context.DbContext.Users
                .Where(u => u.GuildID == Guild.GuildID)
                .Select(u => u.Attempts)
                .SumAsync();
            var guessedAttempts = await _context.DbContext.Users
                .Where(u => u.GuildID == Guild.GuildID)
                .Select(u => u.GuessedAttempts)
                .SumAsync();
            var totalAttempts = 3 * await _context.DbContext.Users
                .Where(u => u.GuildID == Guild.GuildID)
                .CountAsync();
            var unknownUsersQueryable = _context.DbContext.Users
                .Where(u => u.GuildID == Guild.GuildID && u.IsIgnored);
            var unknownUsers = await unknownUsersQueryable.CountAsync();
            var unknownUsedAttempts = await unknownUsersQueryable.SumAsync(u => u.Attempts);

            Attempts = usedAttempts;
            GuessedAttempts = guessedAttempts;
            RemainingAttempts = totalAttempts - usedAttempts;
            UnknownAttempts = 3 * unknownUsers - unknownUsedAttempts;

            BossStatus = imGuild.PredictBossBalance
                .Select(b => (allBosses.FirstOrDefault(bb => bb.BossID == b.bossID), b.balance))
                .ToList();

            return Page();
        }
    }
}
