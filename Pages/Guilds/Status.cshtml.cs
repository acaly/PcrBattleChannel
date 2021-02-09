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
        [Display(Name = "血量矫正系数")]
        [Range(0.5, 2)]
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

            LastUpdateTime = guild.LastCalculation.ToString("MM-dd HH:mm");
            (CurrentLap, CurrentBoss) = ConvLap(guild.BossIndex);
            CurrentLap += 1; //convert to 1-based.
            CurrentBoss += 1;
            CurrentBossRatio = guild.BossDamageRatio;

            var allPlans = await _context.DbContext.GuildBossStatuses
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
                return RedirectToPage("/Home/Index");
            }
            var imGuild = await _context.GetGuild(guild.GuildID);

            if (PlanRatio.HasValue)
            {
                await _context.DbContext.GuildBossStatuses
                    .Where(s => s.GuildID == guild.GuildID && s.IsPlan == true)
                    .DeleteFromQueryAsync();
                foreach (var b in _context.DbContext.Bosses)
                {
                    var s = new GuildBossStatus
                    {
                        GuildID = guild.GuildID,
                        BossID = b.BossID,
                        IsPlan = true,
                        DamageRatio = PlanRatio.Value,
                    };
                    _context.DbContext.GuildBossStatuses.Add(s);
                }
            }

            var stages = await _context.DbContext.BattleStages.OrderBy(s => s.StartLap).ToListAsync();
            FirstLapForStages = stages.Select(s => s.StartLap).ToArray();
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

            //Have to save here to allow calculator to read boss plans.
            await _context.DbContext.SaveChangesAsync();

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

            //2. Refresh users' combo lists.
            var allUserIDs = await _context.DbContext.Users
                .Where(u => u.GuildID == guild.GuildID)
                .ToListAsync();
            var comboCalculator = new FindAllCombos();
            foreach (var user in allUserIDs)
            {
                if (user.IsIgnored) continue;
                var imUser = imGuild.GetUserById(user.Id);

                //Refresh only when needed.
                var attemptCountChanged = imUser.ComboZhouCount != 3 - user.Attempts;
                var zhouChangedSinceLastUpdate = imUser.LastComboCalculation <= guild.LastZhouUpdate;
                if (attemptCountChanged || zhouChangedSinceLastUpdate)
                {
                    var userUsedCharacterIDs = await _context.DbContext.UserCharacterStatuses
                        .Where(s => s.UserID == user.Id)
                        .Select(s => s.CharacterID)
                        .ToListAsync();

                    InheritCombo.ComboInheritInfo inheritComboInfo = null;
                    var userUsedCharacterSet = userUsedCharacterIDs.ToHashSet();
                    if (imUser.SelectedComboIndex != -1)
                    {
                        //should get from IM context instead of DB context
                        inheritComboInfo = await InheritCombo.GetInheritInfo(_context.DbContext, user, userUsedCharacterSet);
                    }

                    comboCalculator.Run(imUser, userUsedCharacterSet, 3 - user.Attempts, inheritComboInfo, user.ComboIncludesDrafts);
                }
            }

            //3. Calculate values.
            await CalcComboValues.RunAllAsync(_context.DbContext, guild, null);

            await _context.DbContext.SaveChangesAsync();

            return RedirectToPage("/Home/Index");
        }
    }
}
