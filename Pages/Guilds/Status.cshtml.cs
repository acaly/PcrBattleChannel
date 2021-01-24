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
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public StatusModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
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
        [Display(Name = "血量矫正系数")]
        [Range(0, 2)]
        public float? PlanRatio { get; set; }

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
            var guild = await _context.Guilds.FirstOrDefaultAsync(g => g.GuildID == user.GuildID.Value);
            return guild;
        }

        private (int lap, int boss) ConvLap(int bossIndex)
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
                return RedirectToPage("/Guild/Index");
            }
            Guild = guild;

            var stages = await _context.BattleStages.OrderBy(s => s.StartLap).ToListAsync();
            FirstLapForStages = stages.Select(s => s.StartLap).ToArray();
            FirstLapForStagesString = JsonConvert.SerializeObject(FirstLapForStages);

            BossNames = new();
            foreach (var s in stages)
            {
                var bosses = await _context.Bosses
                    .Where(b => b.BattleStageID == s.BattleStageID)
                    .OrderBy(b => b.BossID)
                    .Select(b => b.Name)
                    .ToListAsync();
                BossNames.Add(bosses);
            }
            BossNamesString = JsonConvert.SerializeObject(BossNames);

            LastUpdateTime = guild.LastCalculation.ToString("MM-dd HH:mm");
            (CurrentLap, CurrentBoss) = ConvLap(guild.BossIndex);
            CurrentLap += 1; //convert to 1-based.
            CurrentBoss += 1;
            CurrentBossRatio = guild.BossDamageRatio;

            var allPlans = await _context.GuildBossStatuses
                .Where(s => s.GuildID == guild.GuildID && s.IsPlan == true)
                .Select(s => s.DamageRatio)
                .Distinct()
                .ToListAsync();
            if (allPlans.Count == 1)
            {
                PlanRatio = allPlans[0];
            }
            else if (allPlans.Count == 0)
            {
                PlanRatio = 1.0f;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var guild = await CheckUserPrivilege();
            if (guild is null)
            {
                return RedirectToPage("/Guild/Index");
            }

            if (PlanRatio.HasValue)
            {
                await _context.GuildBossStatuses
                    .Where(s => s.GuildID == guild.GuildID && s.IsPlan == true)
                    .DeleteFromQueryAsync();
                foreach (var b in _context.Bosses)
                {
                    var s = new GuildBossStatus
                    {
                        GuildID = guild.GuildID,
                        BossID = b.BossID,
                        IsPlan = true,
                        DamageRatio = PlanRatio.Value,
                    };
                    _context.GuildBossStatuses.Add(s);
                }
            }

            var stages = await _context.BattleStages.OrderBy(s => s.StartLap).ToListAsync();
            FirstLapForStages = stages.Select(s => s.StartLap).ToArray();
            BossNames = new();
            foreach (var s in stages)
            {
                var bosses = await _context.Bosses
                    .Where(b => b.BattleStageID == s.BattleStageID)
                    .OrderBy(b => b.BossID)
                    .Select(b => b.Name)
                    .ToListAsync();
                BossNames.Add(bosses);
            }

            //1. Update guild status.
            var newBossIndex = ConvLap(CurrentLap - 1, CurrentBoss - 1);
            if (!newBossIndex.HasValue)
            {
                return RedirectToPage();
            }
            guild.BossIndex = newBossIndex.Value;
            if (CurrentBossRatio < 0 || CurrentBossRatio >= 1)
            {
                return RedirectToPage();
            }
            guild.BossDamageRatio = CurrentBossRatio;

            await _context.SaveChangesAsync();

            //2. Refresh users' combo lists.
            var allUserIDs = await _context.Users
                .Where(u => u.GuildID == guild.GuildID)
                .Select(u => u.Id)
                .ToListAsync();
            bool combosRequiresSaving = false;
            foreach (var uid in allUserIDs)
            {
                var count = await _context.UserCombos
                    .Where(c => c.UserID == uid && c.SelectedZhou != null)
                    .CountAsync();
                //Refresh only when user has not selected a combo.
                if (count != 1)
                {
                    await _context.UserCombos.Where(c => c.UserID == uid).DeleteFromQueryAsync();
                    await FindAllCombos.Run(_context,
                        await _context.Users.FirstOrDefaultAsync(u => u.Id == uid));
                    combosRequiresSaving = true;
                }
            }
            if (combosRequiresSaving)
            {
                await _context.SaveChangesAsync();
            }

            //3. Calculate values.
            await CalcComboValues.RunAllAsync(_context, guild);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
