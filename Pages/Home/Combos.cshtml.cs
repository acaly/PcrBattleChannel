using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Algorithm;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Home
{
    public class CombosModel : PageModel
    {
        private readonly InMemoryStorageContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public CombosModel(InMemoryStorageContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public List<(string name, InMemoryUser.ComboGroup group, float currentValue, float totalValue)> UserCombo { get; private set; }
        public PcrIdentityUser AppUser { get; private set; }
        public bool IsUserValueApproximate { get; private set; }
        public int TotalUserCombs { get; private set; }
        public List<UserCharacterStatus> UsedCharacters { get; private set; }
        public HashSet<int> UsedCharacterIds { get; private set; }

        public List<Character> AllCharacters { get; private set; }

        [BindProperty]
        [Display(Name = "选择已用角色（不含助战）")]
        public string UsedCharacterString { get; set; }

        [BindProperty]
        public bool UserIncludesDrafts { get; set; }

        public Dictionary<int, Zhou> CachedZhouData { get; } = new();
        public List<(string, float)> BossValues { get; private set; }

        public class SingleComboModel
        {
            public InMemoryUser.Combo Item { get; init; }
            public CombosModel Parent { get; init; }
        }

        public class ComboGroupModel
        {
            public InMemoryUser.ComboGroup Item { get; init; }
            public CombosModel Parent { get; init; }

            public SingleComboModel CreateSingleModel(InMemoryUser.Combo c) => new() { Item = c, Parent = Parent };
        }

        public SingleComboModel CreateSingleModel(InMemoryUser.Combo c) => new() { Item = c, Parent = this };

        public ComboGroupModel CreateGroupModel(InMemoryUser.ComboGroup g) => new() { Item = g, Parent = this };

        private async Task CacheZhouData(int zid)
        {
            if (!CachedZhouData.ContainsKey(zid))
            {
                var z = await _context.DbContext.Zhous
                    .Include(z => z.Boss)
                    .Include(z => z.C1)
                    .Include(z => z.C2)
                    .Include(z => z.C3)
                    .Include(z => z.C4)
                    .Include(z => z.C5)
                    .FirstOrDefaultAsync(z => z.ZhouID == zid);
                CachedZhouData.Add(zid, z);
            }
        }

        private async Task GetUserInfo(PcrIdentityUser user)
        {
            AppUser = user;
            UserIncludesDrafts = user.ComboIncludesDrafts;

            if (user.Attempt1ID.HasValue)
            {
                await _context.DbContext.Entry(user).Reference(u => u.Attempt1).LoadAsync();
                await _context.DbContext.Entry(user.Attempt1).Reference(v => v.Zhou).LoadAsync();
                await CacheZhouData(user.Attempt1.ZhouID);
            }
            if (user.Attempt2ID.HasValue)
            {
                await _context.DbContext.Entry(user).Reference(u => u.Attempt2).LoadAsync();
                await _context.DbContext.Entry(user.Attempt2).Reference(v => v.Zhou).LoadAsync();
                await CacheZhouData(user.Attempt2.ZhouID);
            }
            if (user.Attempt3ID.HasValue)
            {
                await _context.DbContext.Entry(user).Reference(u => u.Attempt3).LoadAsync();
                await _context.DbContext.Entry(user.Attempt3).Reference(v => v.Zhou).LoadAsync();
                await CacheZhouData(user.Attempt3.ZhouID);
            }
        }

        private async Task GetAllCharacters(PcrIdentityUser user)
        {
            var allZhous = _context.DbContext.Zhous
                .Where(z => z.GuildID == user.GuildID);
            var cid = new HashSet<int>();
            foreach (var z in allZhous)
            {
                if (z.C1ID.HasValue) cid.Add(z.C1ID.Value);
                if (z.C2ID.HasValue) cid.Add(z.C2ID.Value);
                if (z.C3ID.HasValue) cid.Add(z.C3ID.Value);
                if (z.C4ID.HasValue) cid.Add(z.C4ID.Value);
                if (z.C5ID.HasValue) cid.Add(z.C5ID.Value);
            }
            AllCharacters = new();
            foreach (var c in cid)
            {
                if (!await _context.DbContext.UserCharacterConfigs
                    .Include(cc => cc.CharacterConfig)
                    .AnyAsync(cc => cc.UserID == user.Id && cc.CharacterConfig.CharacterID == c))
                {
                    continue;
                }
                AllCharacters.Add(await _context.DbContext.Characters.FirstOrDefaultAsync(cc => cc.CharacterID == c));
            }
            AllCharacters.Sort((c1, c2) => Math.Sign(c1.Range - c2.Range));
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return Redirect("/");
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue)
            {
                return Redirect("/");
            }
            var imGuild = await _context.GetGuildAsync(user.GuildID.Value);
            var imUser = imGuild.GetUserById(user.Id);
            IsUserValueApproximate = imUser.IsValueApproximate;
            TotalUserCombs = imUser.TotalComboCount;

            await GetUserInfo(user);
            var allBosses = (await _context.DbContext.Bosses.ToListAsync()).ToDictionary(b => b.BossID);
            var bossDict = new Dictionary<Boss, float>();
            void AddBoss(int bossID, float value)
            {
                if (bossID == 0) return;
                var boss = allBosses[bossID];
                bossDict.TryGetValue(boss, out var oldVal);
                bossDict[boss] = oldVal + value;
            }

            UserCombo = new();
            var comboNameBuilder = new StringBuilder();
            for (int i = 0; i < imUser.ComboGroupCount; ++i)
            {
                var g = imUser.GetComboGroup(i);
                if (g.Count == 0) continue;

                float currentValue = 0, totalValue = 0;
                for (int j = 0; j < g.Count; ++j)
                {
                    var c = g.GetCombo(j);
                    currentValue += c.CurrentValue;
                    totalValue += c.TotalValue;
                    for (int k = 0; k < c.ZhouCount; ++k)
                    {
                        await CacheZhouData(c.GetZhouVariant(k).ZhouID);
                    }
                }

                comboNameBuilder.Clear();
                var (b1, b2, b3) = g.GetCombo(0).BossIDTuple;
                if (b1 != 0)
                {
                    comboNameBuilder.Append(allBosses[b1].ShortName);
                    AddBoss(b1, currentValue);
                }
                if (b2 != 0)
                {
                    comboNameBuilder.Append(" + ");
                    comboNameBuilder.Append(allBosses[b2].ShortName);
                    AddBoss(b2, currentValue);
                }
                if (b3 != 0)
                {
                    comboNameBuilder.Append(" + ");
                    comboNameBuilder.Append(allBosses[b3].ShortName);
                    AddBoss(b3, currentValue);
                }
                UserCombo.Add((comboNameBuilder.ToString(), g, currentValue, totalValue));
            }

            UserCombo.Sort((a, b) => MathF.Sign(b.currentValue - a.currentValue));

            BossValues = bossDict
                .OrderBy(bb => bb.Key.BossID)
                .Select(bb => ($"{bb.Key.ShortName} ({bb.Key.Name})", bb.Value))
                .ToList();

            UsedCharacters = await _context.DbContext.UserCharacterStatuses
                .Include(s => s.Character)
                .Where(s => s.UserID == user.Id)
                .ToListAsync();
            UsedCharacters.Sort((a, b) => Math.Sign(a.Character.Range - b.Character.Range));
            UsedCharacterIds = UsedCharacters.Select(c => c.CharacterID).ToHashSet();
            UsedCharacterString = string.Join(',', UsedCharacterIds);

            await GetAllCharacters(user);

            return Page();
        }

        //Ajax
        public async Task<IActionResult> OnGetGroupPartialAsync(long time, int index)
        {
            if (time == 0 || index < 0)
            {
                return StatusCode(400);
            }
            if (!_signInManager.IsSignedIn(User))
            {
                return StatusCode(400);
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue)
            {
                return StatusCode(400);
            }
            var imGuild = await _context.GetGuildAsync(user.GuildID.Value);
            var imUser = imGuild.GetUserById(user.Id);
            if (time != imUser.LastComboCalculation.Ticks || index >= imUser.ComboGroupCount)
            {
                return StatusCode(400);
            }

            await GetUserInfo(user);

            var group = imUser.GetComboGroup(index);
            foreach (var c in group.Combos)
            {
                await CacheZhouData(c.GetZhouVariant(0).ZhouID);
                await CacheZhouData(c.GetZhouVariant(1).ZhouID);
                await CacheZhouData(c.GetZhouVariant(2).ZhouID);
            }
            return Partial("_Combo_ComboGroupPartial", CreateGroupModel(group));
        }

        public async Task<IActionResult> OnPostRefreshAsync()
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            if (!_signInManager.IsSignedIn(User))
            {
                return Redirect("/");
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue)
            {
                return Redirect("/");
            }
            var guild = await _context.DbContext.Guilds.FirstOrDefaultAsync(g => g.GuildID == user.GuildID.Value);
            if (guild is null)
            {
                return Redirect("/");
            }
            Console.WriteLine($"validate {timer.ElapsedMilliseconds} ms");
            var imGuild = await _context.GetGuildAsync(guild.GuildID);
            var imUser = imGuild.GetUserById(user.Id);
            var comboCalculator = new FindAllCombos();

            var userUsedCharacterIDs = await _context.DbContext.UserCharacterStatuses
                .Where(s => s.UserID == user.Id)
                .Select(s => s.CharacterID)
                .ToListAsync();

            Console.WriteLine($"prepare {timer.ElapsedMilliseconds} ms");
            comboCalculator.Run(imUser, userUsedCharacterIDs.ToHashSet(), 3 - user.Attempts, null, user.ComboIncludesDrafts);
            Console.WriteLine($"combo {timer.ElapsedMilliseconds} ms");
            await CalcComboValues.RunSingleAsync(_context.DbContext, guild, imGuild, user);
            Console.WriteLine($"value {timer.ElapsedMilliseconds} ms");
            await _context.DbContext.SaveChangesAsync();
            Console.WriteLine($"save {timer.ElapsedMilliseconds} ms");

            Console.WriteLine($"total variants: {imGuild.ZhouVariants.Count()}");
            Console.WriteLine($"total combos: {imUser.TotalComboCount}");

            return RedirectToPage();
        }

        //Ajax
        public async Task<IActionResult> OnPostConfirmAsync()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return StatusCode(400);
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue)
            {
                return StatusCode(400);
            }

            if (user.GuessedAttempts == 0 || user.IsIgnored)
            {
                return StatusCode(400);
            }

            user = await _context.DbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            user.GuessedAttempts = 0;
            user.LastConfirm = TimeZoneHelper.BeijingNow;

            _context.DbContext.Users.Update(user);
            await _context.DbContext.SaveChangesAsync();

            return StatusCode(200);
        }

        //Ajax
        public async Task<IActionResult> OnPostUpdateStatusAsync()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return StatusCode(400);
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue)
            {
                return StatusCode(400);
            }
            var imGuild = await _context.GetGuildAsync(user.GuildID.Value);
            var imUser = imGuild.GetUserById(user.Id);
            await GetUserInfo(user);

            await _context.DbContext.UserCharacterStatuses
                .Where(s => s.UserID == user.Id)
                .DeleteFromQueryAsync();
            UsedCharacters = new();
            try
            {
                var list = UsedCharacterString?.Split(',') ?? Array.Empty<string>();
                foreach (var item in list)
                {
                    var cid = int.Parse(item);
                    var s = new UserCharacterStatus
                    {
                        UserID = user.Id,
                        //Page will need this reference, so load it here.
                        Character = await _context.DbContext.Characters.FirstAsync(c => c.CharacterID == cid),
                        CharacterID = cid,
                    };
                    UsedCharacters.Add(s);
                    _context.DbContext.UserCharacterStatuses.Add(s);
                }
                user.Attempts = list.Length switch
                {
                    > 10 => 3,
                    > 5 => 2,
                    > 0 => 1,
                    _ => 0,
                };
                user.Attempt1ID = user.Attempt2ID = user.Attempt3ID = null;
                user.Attempt1Borrow = user.Attempt2Borrow = user.Attempt3Borrow = null;
                user.GuessedAttempts = 0;
                user.IsIgnored = false;
                user.LastConfirm = TimeZoneHelper.BeijingNow;
                imUser.LastComboCalculation = default;
            }
            catch
            {
                return StatusCode(400);
            }
            await _context.DbContext.SaveChangesAsync();

            await GetAllCharacters(user);

            UsedCharacters.Sort((a, b) => Math.Sign(a.Character.Range - b.Character.Range));
            UsedCharacterIds = UsedCharacters.Select(c => c.CharacterID).ToHashSet();

            return Partial("_Combo_StatusPartial", this);
        }

        //Ajax
        public async Task<IActionResult> OnPostBorrowSwapAsync(long time, int? id)
        {
            if (!id.HasValue)
            {
                return StatusCode(400);
            }
            if (!_signInManager.IsSignedIn(User))
            {
                return StatusCode(400);
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue)
            {
                return StatusCode(400);
            }
            var imGuild = await _context.GetGuildAsync(user.GuildID.Value);
            var imUser = imGuild.GetUserById(user.Id);
            if (time == 0 || time != imUser.LastComboCalculation.Ticks ||
                id.Value < 0 || id.Value >= imUser.TotalComboCount)
            {
                return StatusCode(400);
            }
            await GetUserInfo(user);

            var c = imUser.GetCombo(id.Value);
            c.SwitchBorrow();

            SingleComboModel model = CreateSingleModel(c);
            await CacheZhouData(c.GetZhouVariant(0).ZhouID);
            await CacheZhouData(c.GetZhouVariant(1).ZhouID);
            await CacheZhouData(c.GetZhouVariant(2).ZhouID);

            return Partial("_Combo_ComboPartial", model);
        }

        public async Task<IActionResult> OnPostSelectAsync(long time, int? combo, int? zhou)
        {
            if (!combo.HasValue || !zhou.HasValue || zhou.Value < -1 || zhou.Value > 2)
            {
                return StatusCode(400);
            }
            if (!_signInManager.IsSignedIn(User))
            {
                return StatusCode(400);
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue)
            {
                return StatusCode(400);
            }
            var imGuild = await _context.GetGuildAsync(user.GuildID.Value);
            var imUser = imGuild.GetUserById(user.Id);
            if (time == 0 || time != imUser.LastComboCalculation.Ticks ||
                combo.Value < 0 || combo.Value >= imUser.TotalComboCount)
            {
                return StatusCode(400);
            }

            if (zhou == -1)
            {
                imUser.SelectedComboIndex = imUser.SelectedComboZhouIndex = -1;
            }
            else
            {
                imUser.SelectedComboIndex = combo.Value;
                imUser.SelectedComboZhouIndex = zhou.Value;
            }

            return StatusCode(200);
        }

        public async Task<IActionResult> OnPostSetDraftAsync()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return StatusCode(400);
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue)
            {
                return StatusCode(400);
            }

            user.ComboIncludesDrafts = UserIncludesDrafts;
            await _context.DbContext.SaveChangesAsync();

            return StatusCode(200);
        }
    }
}
