using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PcrBattleChannel.Algorithm;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Guilds
{
    public class StatusModel : PageModel
    {
        private readonly InMemoryStorageContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public StatusModel(InMemoryStorageContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public Guild Guild { get; set; }

        public int[] FirstLapForStages { get; set; }
        public string FirstLapForStagesString { get; set; }
        public List<List<string>> BossNames { get; set; }
        public string BossNamesString { get; set; }

        public string LastUpdateTime { get; set; }

        [BindProperty]
        [Display(Name = "伤害矫正系数")]
        [Range(0.5, 2)]
        public float PlanRatio { get; set; }

        [BindProperty]
        [Display(Name = "当前周目")]
        [Range(1, 100)]
        public int CurrentLap { get; set; }

        [BindProperty]
        [Display(Name = "当前Boss")]
        [Range(1, 5)]
        public int CurrentBoss { get; set; }

        [BindProperty]
        [Display(Name = "当前Boss血量百分比")]
        [Range(0, 1)]
        public float CurrentBossRatio { get; set; }

        private async Task<Guild> CheckUserPrivilege()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return null;
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue || !user.IsGuildAdmin)
            {
                return null;
            }
            var guild = await _context.DbContext.Guilds.FirstOrDefaultAsync(g => g.GuildID == user.GuildID.Value);
            return guild;
        }

        private (int lap, int boss) ConvLap(int bossIndex)
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
            return (lap, bossIndex);
        }

        private int? ConvLap(int lap, int boss)
        {
            if (lap < 0 || boss < 0) return null;
            int stage = 0, ilap = 0, result = 0;
            while (lap > ilap)
            {
                result += BossNames[stage].Count;
                ilap += 1;
                if (stage + 1 < FirstLapForStages.Length && ilap >= FirstLapForStages[stage + 1])
                {
                    stage += 1;
                }
            }
            if (boss >= BossNames[stage].Count) return null;
            return result + boss;
        }

        public async Task<IActionResult> OnGet()
        {
            var guild = await CheckUserPrivilege();
            if (guild is null)
            {
                return RedirectToPage("/Home/Index");
            }
            Guild = guild;
            var imGuild = await _context.GetGuildAsync(guild.GuildID);

            var stages = await _context.DbContext.BattleStages.OrderBy(s => s.StartLap).ToListAsync();
            FirstLapForStages = stages.Select(s => s.StartLap).ToArray();
            FirstLapForStagesString = JsonConvert.SerializeObject(FirstLapForStages);

            BossNames = new();
            foreach (var s in stages)
            {
                var bosses = await _context.DbContext.Bosses
                    .Where(b => b.BattleStageID == s.BattleStageID)
                    .OrderBy(b => b.BossID)
                    .Select(b => b.Name)
                    .ToListAsync();
                BossNames.Add(bosses);
            }
            BossNamesString = JsonConvert.SerializeObject(BossNames);

            LastUpdateTime = imGuild.LastCalculation.ToString("MM-dd HH:mm");
            (CurrentLap, CurrentBoss) = ConvLap(guild.BossIndex);
            CurrentLap += 1; //convert to 1-based.
            CurrentBoss += 1;
            CurrentBossRatio = guild.BossDamageRatio;

            PlanRatio = guild.DamageCoefficient;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var guild = await CheckUserPrivilege();
            if (guild is null)
            {
                return RedirectToPage("/Home/Index");
            }
            Console.WriteLine($"validate {timer.ElapsedMilliseconds} ms");
            var imGuild = await _context.GetGuildAsync(guild.GuildID);

            guild.DamageCoefficient = PlanRatio;

            //TODO cache
            var stages = await _context.DbContext.BattleStages.OrderBy(s => s.StartLap).ToListAsync();
            FirstLapForStages = stages.Select(s => s.StartLap).ToArray();
            BossNames = new();
            foreach (var s in stages)
            {
                var bosses = await _context.DbContext.Bosses
                    .Where(b => b.BattleStageID == s.BattleStageID)
                    .Select(b => (string)null) //We only need count for boss index conversion.
                    .ToListAsync();
                BossNames.Add(bosses);
            }

            //1. Update guild status.
            var newBossIndex = ConvLap(CurrentLap - 1, CurrentBoss - 1);
            if (!newBossIndex.HasValue)
            {
                return RedirectToPage();
            }
            if (CurrentBossRatio < 0 || CurrentBossRatio >= 1)
            {
                return RedirectToPage();
            }
            guild.BossIndex = newBossIndex.Value;
            guild.BossDamageRatio = CurrentBossRatio;

            //This is the only save we need for this request.
            await _context.DbContext.SaveChangesAsync();
            Console.WriteLine($"guild progress update {timer.ElapsedMilliseconds} ms");

            //2. Refresh users' combo lists.
            var comboCalculator = new FindAllCombos();
            await comboCalculator.UpdateGuildAsync(_context, guild.GuildID, imGuild);
            Console.WriteLine($"user combo refresh {timer.ElapsedMilliseconds} ms");

            //3. Calculate values.
            await CalcComboValues.RunAllAsync(_context.DbContext, guild, imGuild);
            Console.WriteLine($"value calculation {timer.ElapsedMilliseconds} ms");

            return RedirectToPage("/Home/Index");
        }

        public async Task<IActionResult> OnPostCalcAsync()
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var guild = await CheckUserPrivilege();
            if (guild is null)
            {
                return RedirectToPage("/Home/Index");
            }
            Console.WriteLine($"validate {timer.ElapsedMilliseconds} ms");
            var imGuild = await _context.GetGuildAsync(guild.GuildID);

            //1. Refresh users' combo lists.
            var comboCalculator = new FindAllCombos();
            await comboCalculator.UpdateGuildAsync(_context, guild.GuildID, imGuild);
            Console.WriteLine($"user combo refresh {timer.ElapsedMilliseconds} ms");

            //2. Calculate values.
            await CalcComboValues.RunAllAsync(_context.DbContext, guild, imGuild);
            Console.WriteLine($"value calculation {timer.ElapsedMilliseconds} ms");

            return RedirectToPage("/Home/Index");
        }
    }
}
