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
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public IndexModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public Guild Guild { get; private set; }

        public int[] FirstLapForStages { get; private set; }
        public List<List<string>> BossNames { get; private set; }
        public List<List<string>> BossShortNames { get; private set; }
        public int Attempts { get; private set; }
        public int GuessedAttempts { get; private set; }
        public int RemainingAttempts { get; private set; }
        public List<GuildBossStatus> BossStatus { get; private set; }

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
                if (stage + 1 < FirstLapForStages.Length && lap >= FirstLapForStages[stage + 1])
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
            if (!user.GuildID.HasValue)
            {
                return Page();
            }

            Guild = await _context.Guilds.FirstOrDefaultAsync(g => g.GuildID == user.GuildID.Value);

            var stages = await _context.BattleStages.OrderBy(s => s.StartLap).ToListAsync();
            FirstLapForStages = stages.Select(s => s.StartLap).ToArray();

            BossNames = new();
            BossShortNames = new();
            foreach (var s in stages)
            {
                var names = await _context.Bosses
                    .Where(b => b.BattleStageID == s.BattleStageID)
                    .OrderBy(b => b.BossID)
                    .Select(b => b.Name)
                    .ToListAsync();
                BossNames.Add(names);
                var shortNames = await _context.Bosses
                    .Where(b => b.BattleStageID == s.BattleStageID)
                    .OrderBy(b => b.BossID)
                    .Select(b => b.ShortName)
                    .ToListAsync();
                BossShortNames.Add(shortNames);
            }

            var usedAttempts = await _context.Users
                .Where(u => u.GuildID == Guild.GuildID)
                .Select(u => u.Attempts)
                .SumAsync();
            var guessedAttempts = await _context.Users
                .Where(u => u.GuildID == Guild.GuildID)
                .Select(u => u.GuessedAttempts)
                .SumAsync();
            var totalAttempts = 3 * await _context.Users
                .Where(u => u.GuildID == Guild.GuildID)
                .CountAsync();
            Attempts = usedAttempts;
            GuessedAttempts = guessedAttempts;
            RemainingAttempts = totalAttempts - usedAttempts;

            BossStatus = await _context.GuildBossStatuses
                .Include(s => s.Boss)
                .Where(s => s.GuildID == Guild.GuildID && s.IsPlan == false)
                .OrderBy(s => s.BossID)
                .ToListAsync();

            return Page();
        }
    }
}
