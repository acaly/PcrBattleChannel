using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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

        public List<(string name, List<UserCombo> list, float value, float netValue)> UserCombo { get; set; }
        public PcrIdentityUser AppUser { get; set; }
        public List<UserCharacterStatus> UsedCharacters { get; set; }
        public HashSet<int> UsedCharacterIds { get; set; }

        public List<Character> AllCharacters { get; set; }

        [BindProperty]
        [Display(Name = "选择已用角色（不含助战）")]
        public string UsedCharacterString { get; set; }

        [BindProperty]
        public bool UserIncludesDrafts { get; set; }

        public Dictionary<int, Zhou> CachedZhouData { get; set; } = new();

        public List<(string, float)> BossValues { get; set; } = new();

        public class SingleComboModel
        {
            public UserCombo Item { get; init; }
            public CombosModel Parent { get; init; }
        }

        public SingleComboModel CreateSingleModel(UserCombo c) => new() { Item = c, Parent = this };

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
            await GetUserInfo(user);

            var rawList = await _context.DbContext.UserCombos
                .Include(u => u.Zhou1)
                .Include(u => u.Zhou2)
                .Include(u => u.Zhou3)
                .Where(u => u.UserID == user.Id)
                .ToListAsync();
            foreach (var c in rawList)
            {
                if (c.Zhou1ID.HasValue)
                {
                    await _context.DbContext.Entry(c.Zhou1).Reference(z => z.ZhouVariant).LoadAsync();
                    await _context.DbContext.Entry(c.Zhou1.ZhouVariant).Reference(v => v.Zhou).LoadAsync();
                    await CacheZhouData(c.Zhou1.ZhouVariant.ZhouID);
                }
                if (c.Zhou2ID.HasValue)
                {
                    await _context.DbContext.Entry(c.Zhou2).Reference(z => z.ZhouVariant).LoadAsync();
                    await _context.DbContext.Entry(c.Zhou2.ZhouVariant).Reference(v => v.Zhou).LoadAsync();
                    await CacheZhouData(c.Zhou2.ZhouVariant.ZhouID);
                }
                if (c.Zhou3ID.HasValue)
                {
                    await _context.DbContext.Entry(c.Zhou3).Reference(z => z.ZhouVariant).LoadAsync();
                    await _context.DbContext.Entry(c.Zhou3.ZhouVariant).Reference(v => v.Zhou).LoadAsync();
                    await CacheZhouData(c.Zhou3.ZhouVariant.ZhouID);
                }
            }

            static string GetComboGroupedName(UserCombo c)
            {
                var b1 = c.Zhou1?.ZhouVariant.Zhou.Boss;
                var b2 = c.Zhou2?.ZhouVariant.Zhou.Boss;
                var b3 = c.Zhou3?.ZhouVariant.Zhou.Boss;
                if (b1 is null) return "空";
                if (b2 is null) return $"{b1.ShortName}";
                if (b3 is null) return $"{b1.ShortName} + {b2.ShortName}";
                return $"{b1?.ShortName} + {b2.ShortName} + {b3.ShortName}";
            }
            UserCombo = rawList
                .GroupBy(GetComboGroupedName)
                .Select(g =>
                {
                    var groupList = g.ToList();
                    groupList.Sort((c1, c2) => MathF.Sign(c2.Value - c1.Value));
                    var sumValue = groupList.Sum(c => c.Value);
                    var sumNetValue = groupList.Sum(c => c.NetValue);
                    return (g.Key, groupList, sumValue, sumNetValue);
                })
                .ToList();
            UserCombo.Sort((a, b) => MathF.Sign(b.value - a.value));

            var bossDict = new Dictionary<Boss, float>();
            void AddBoss(UserZhouVariant uzv, float value)
            {
                var b = uzv?.ZhouVariant.Zhou.Boss;
                if (b is null) return;
                bossDict.TryGetValue(b, out var oldVal);
                bossDict[b] = oldVal + value;
            }
            foreach (var c in rawList)
            {
                AddBoss(c.Zhou1, c.Value);
                AddBoss(c.Zhou2, c.Value);
                AddBoss(c.Zhou3, c.Value);
            }
            BossValues = bossDict
                .OrderBy(bb => bb.Key.BossID)
                .Select(bb => ($"{bb.Key.ShortName} ({bb.Key.Name})", bb.Value))
                .ToList();

            UsedCharacters = await _context.DbContext.UserCharacterStatuses
                .Include(s => s.Character)
                .Where(s => s.UserID == user.Id && s.IsUsed == true)
                .ToListAsync();
            UsedCharacters.Sort((a, b) => Math.Sign(a.Character.Range - b.Character.Range));
            UsedCharacterIds = UsedCharacters.Select(c => c.CharacterID).ToHashSet();
            UsedCharacterString = string.Join(',', UsedCharacterIds);

            await GetAllCharacters(user);

            return Page();
        }

        public async Task<IActionResult> OnPostRefreshAsync()
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
            var guild = await _context.DbContext.Guilds.FirstOrDefaultAsync(g => g.GuildID == user.GuildID.Value);
            if (guild is null)
            {
                return Redirect("/");
            }

            var imGuild = await _context.GetGuild(guild.GuildID);
            var imUser = imGuild.GetUserById(user.Id);
            var comboCalculator = new FindAllCombos();

            var userUsedCharacterIDs = await _context.DbContext.UserCharacterStatuses
                .Where(s => s.UserID == user.Id)
                .Select(s => s.CharacterID)
                .ToListAsync();

            comboCalculator.Run(imUser, userUsedCharacterIDs.ToHashSet(), 3 - user.Attempts, null, user.ComboIncludesDrafts);
            await CalcComboValues.RunSingleAsync(_context.DbContext, guild, user, null);
            await _context.DbContext.SaveChangesAsync();

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
                        IsUsed = true,
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
                user.LastComboUpdate = default;
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
        public async Task<IActionResult> OnPostBorrowSwapAsync(int? id)
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
            await GetUserInfo(user);

            var c = await _context.DbContext.UserCombos
                .Include(u => u.Zhou1)
                .Include(u => u.Zhou2)
                .Include(u => u.Zhou3)
                .FirstOrDefaultAsync(u => u.UserComboID == id.Value);

            try
            {
                var borrowLists = c.BorrowInfo;
                var index = borrowLists.IndexOf(';');
                if (index != -1)
                {
                    c.BorrowInfo = borrowLists.Substring(index + 1) + ";" + borrowLists.Substring(0, index);
                    _context.DbContext.Update(c);
                    await _context.DbContext.SaveChangesAsync();
                }
            }
            catch
            {
                return StatusCode(400);
            }

            SingleComboModel model;
            {
                if (c is null || c.UserID != user.Id)
                {
                    return StatusCode(400);
                }
                if (c.Zhou1ID.HasValue)
                {
                    await _context.DbContext.Entry(c.Zhou1).Reference(z => z.ZhouVariant).LoadAsync();
                    await _context.DbContext.Entry(c.Zhou1.ZhouVariant).Reference(v => v.Zhou).LoadAsync();
                    await CacheZhouData(c.Zhou1.ZhouVariant.ZhouID);
                }
                if (c.Zhou2ID.HasValue)
                {
                    await _context.DbContext.Entry(c.Zhou2).Reference(z => z.ZhouVariant).LoadAsync();
                    await _context.DbContext.Entry(c.Zhou2.ZhouVariant).Reference(v => v.Zhou).LoadAsync();
                    await CacheZhouData(c.Zhou2.ZhouVariant.ZhouID);
                }
                if (c.Zhou3ID.HasValue)
                {
                    await _context.DbContext.Entry(c.Zhou3).Reference(z => z.ZhouVariant).LoadAsync();
                    await _context.DbContext.Entry(c.Zhou3.ZhouVariant).Reference(v => v.Zhou).LoadAsync();
                    await CacheZhouData(c.Zhou3.ZhouVariant.ZhouID);
                }
                model = CreateSingleModel(c);
            }

            return Partial("_Combo_ComboPartial", model);
        }

        public async Task<IActionResult> OnPostSelectAsync(int? combo, int? zhou)
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

            var c = await _context.DbContext.UserCombos
                .FirstOrDefaultAsync(u => u.UserComboID == combo.Value);
            if (c is null || c.UserID != user.Id)
            {
                return StatusCode(400);
            }

            if (zhou != -1)
            {
                var zhouid = zhou.Value switch
                {
                    0 => c.Zhou1ID,
                    1 => c.Zhou2ID,
                    2 => c.Zhou3ID,
                    _ => null,
                };
                if (!zhouid.HasValue)
                {
                    return StatusCode(400);
                }
            }

            var lastSelected = await _context.DbContext.UserCombos
                .Where(c => c.UserID == user.Id && c.SelectedZhou != null)
                .ToListAsync();
            foreach (var last in lastSelected)
            {
                last.SelectedZhou = null;
                _context.DbContext.UserCombos.Update(last);
            }
            c.SelectedZhou = zhou == -1 ? null : zhou.Value;
            _context.DbContext.UserCombos.Update(c);

            await _context.DbContext.SaveChangesAsync();
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
