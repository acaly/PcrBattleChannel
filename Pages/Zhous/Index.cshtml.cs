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

namespace PcrBattleChannel.Pages.Zhous
{
    public class IndexModel : PageModel
    {
        private readonly InMemoryStorageContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public IndexModel(InMemoryStorageContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public bool IsAdmin { get; set; }
        public bool IsOwner { get; set; }
        public Guild Guild { get; set; }
        public List<(Zhou zhou, List<ZhouVariant> variants)> Zhou { get; set; }
        public HashSet<int> UserZhouSettings { get; set; }
        public List<string> AllBosses { get; private set; }
        public Dictionary<int, CharacterConfig> AllConfigs { get; private set; }

        public enum Action
        {
            Show,
            Match,
            UndoMatch,
            Draft,
            UndoDraft,
            Delete,
        }

        //Filtering.

        [BindProperty]
        public Action FilterAction { get; set; } = Action.Show;

        [BindProperty(SupportsGet = true)]
        public string Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public List<string> Bosses { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string Configs { get; set; }

        private async Task<PcrIdentityUser> CheckUserPrivilege()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return null;
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue)
            {
                return null;
            }
            Guild = await _context.DbContext.Guilds.FirstOrDefaultAsync(g => g.GuildID == user.GuildID);
            if (Guild is null)
            {
                return null;
            }
            IsOwner = Guild.OwnerID == user.Id;
            return user;
        }

        private async Task DoFilter(List<Zhou> allZhous, InMemoryGuild imGuild)
        {
            Zhou = new();
            var words = Search?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>();
            List<int>[] configs;
            Dictionary<int, int> characterToConfig;
            try
            {
                configs = Configs?
                    .Split(';')
                    .Select(g => g.Split(',').Select(i => int.Parse(i)).ToList())
                    .ToArray() ?? Array.Empty<List<int>>();

                characterToConfig = await _context.DbContext.CharacterConfigs
                    .Where(cc => cc.GuildID == imGuild.GuildID && cc.Kind == CharacterConfigKind.Default)
                    .ToDictionaryAsync(cc => cc.CharacterID, cc => cc.CharacterConfigID);
            }
            catch
            {
                return;
            }

            var list = new List<ZhouVariant>();
            foreach (var z in allZhous)
            {
                //Search
                if (!words.All(word => (z.Name?.Contains(word) ?? false) || (z.Description?.Contains(word) ?? false)))
                {
                    continue;
                }

                //Bosses
                if (Bosses.Count != 0 && !Bosses.Contains(z.Boss.ShortName))
                {
                    continue;
                }

                list ??= new List<ZhouVariant>();

                //Configs
                foreach (var v in z.Variants)
                {
                    //Get from IM context (save 1 db query).
                    if (!imGuild.TryGetZhouVariantById(v.ZhouVariantID, out var imv)) continue;
                    var vconfigs = imv.CharacterConfigIDs.SelectMany(list => list.SelectMany(list2 => list2)).ToHashSet();
                    foreach (var vc in imv.CharacterIDs)
                    {
                        if (characterToConfig.TryGetValue(vc, out var vcc))
                        {
                            vconfigs.Add(vcc);
                        }
                    }
                    bool foundAll = true;
                    foreach (var g in configs)
                    {
                        bool found = false;
                        foreach (var cc in g)
                        {
                            if (vconfigs.Contains(cc))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            foundAll = false;
                            break;
                        }
                    }
                    if (foundAll)
                    {
                        list.Add(v);
                    }
                }

                if (list.Count == 0) continue;
                Zhou.Add((z, list));
                list = null;
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await CheckUserPrivilege();
            if (user is null)
            {
                return RedirectToPage("/Index");
            }
            IsAdmin = user.IsGuildAdmin;
            var imGuild = await _context.GetGuildAsync(user.GuildID.Value);
            var imUser = imGuild.GetUserById(user.Id);

            AllBosses = await _context.DbContext.Bosses
                .OrderBy(b => b.BossID)
                .Select(b => b.ShortName)
                .ToListAsync();

            //Are we using too many includes?
            var allZhous = await _context.DbContext.Zhous
                .Include(z => z.Boss)
                .Include(z => z.C1)
                .Include(z => z.C2)
                .Include(z => z.C3)
                .Include(z => z.C4)
                .Include(z => z.C5)
                .Include(z => z.Guild)
                .Include(z => z.Variants)
                .Where(z => z.GuildID == user.GuildID.Value)
                .OrderBy(z => z.BossID)
                .ToListAsync();

            UserZhouSettings = imGuild.ZhouVariants
                .Where(zv => zv.UserData[imUser.Index].BorrowPlusOne != 0)
                .Select(zv => zv.ZhouID)
                .ToHashSet();

            AllConfigs = await _context.DbContext.CharacterConfigs
                .Include(cc => cc.Character)
                .Where(cc => cc.GuildID == user.GuildID.Value)
                .ToDictionaryAsync(cc => cc.CharacterConfigID);

            await DoFilter(allZhous, imGuild);

            return Page();
        }

        public async Task<IActionResult> OnPostFilterAsync()
        {
            var user = await CheckUserPrivilege();
            if (user is null)
            {
                return RedirectToPage("/Index");
            }
            if (FilterAction == Action.Show)
            {
                //We want to allow user to copy full urls with parameters. So redirect to GET.
                return RedirectToPage(new
                {
                    Search,
                    Bosses,
                    Configs,
                });
            }
            IsAdmin = user.IsGuildAdmin;
            var imGuild = await _context.GetGuildAsync(user.GuildID.Value);
            var imUser = imGuild.GetUserById(user.Id);

            AllBosses = await _context.DbContext.Bosses
                .OrderBy(b => b.BossID)
                .Select(b => b.ShortName)
                .ToListAsync();

            //Are we using too many includes?
            var allZhous = await _context.DbContext.Zhous
                .Include(z => z.Boss)
                .Include(z => z.C1)
                .Include(z => z.C2)
                .Include(z => z.C3)
                .Include(z => z.C4)
                .Include(z => z.C5)
                .Include(z => z.Guild)
                .Include(z => z.Variants)
                .Where(z => z.GuildID == user.GuildID)
                .OrderBy(z => z.BossID)
                .ToListAsync();

            //UserZhouSettings = imGuild.ZhouVariants
            //    .Where(zv => zv.UserData[imUser.Index].BorrowPlusOne != 0)
            //    .Select(zv => zv.ZhouID)
            //    .ToHashSet();
            //
            //AllConfigs = await _context.DbContext.CharacterConfigs
            //    .Include(cc => cc.Character)
            //    .Where(cc => cc.GuildID == user.GuildID.Value)
            //    .ToDictionaryAsync(cc => cc.CharacterConfigID);

            await DoFilter(allZhous, imGuild);

            switch (FilterAction)
            {
            case Action.Match:
                var userAllConfigs = await _context.DbContext.UserCharacterConfigs
                    .Include(c => c.CharacterConfig)
                    .Where(c => c.UserID == user.Id)
                    .ToListAsync();
                var userCharacters = userAllConfigs
                    .Where(c => c.CharacterConfig.Kind == default)
                    .Select(c => c.CharacterConfig.CharacterID)
                    .ToHashSet();
                var userAllConfigIDs = userAllConfigs.Select(c => c.CharacterConfigID).ToHashSet();

                imUser.ClearComboList(null);
                foreach (var v in Zhou.SelectMany(z => z.variants))
                {
                    imUser.MatchZhouVariant(v.ZhouVariantID, userCharacters, userAllConfigIDs, false);
                }
                UserZhouSettings = imGuild.ZhouVariants
                    .Where(zv => zv.UserData[imUser.Index].BorrowPlusOne != 0)
                    .Select(zv => zv.ZhouID)
                    .ToHashSet();
                //return Page();
                return RedirectToPage(new
                {
                    Search,
                    Bosses,
                    Configs,
                });
            case Action.UndoMatch:
                var emptyIDList = new HashSet<int>();
                imUser.ClearComboList(null);
                foreach (var v in Zhou.SelectMany(z => z.variants))
                {
                    imUser.MatchZhouVariant(v.ZhouVariantID, emptyIDList, emptyIDList, false);
                }
                UserZhouSettings = imGuild.ZhouVariants
                    .Where(zv => zv.UserData[imUser.Index].BorrowPlusOne != 0)
                    .Select(zv => zv.ZhouID)
                    .ToHashSet();
                //return Page();
                return RedirectToPage(new
                {
                    Search,
                    Bosses,
                    Configs,
                });
            case Action.Draft:
                foreach (var v in Zhou.SelectMany(z => z.variants))
                {
                    v.IsDraft = true;
                }
                await _context.DbContext.SaveChangesAsync();
                //return Page();
                return RedirectToPage(new
                {
                    Search,
                    Bosses,
                    Configs,
                });
            case Action.UndoDraft:
                foreach (var v in Zhou.SelectMany(z => z.variants))
                {
                    v.IsDraft = false;
                }
                await _context.DbContext.SaveChangesAsync();
                //return Page();
                return RedirectToPage(new
                {
                    Search,
                    Bosses,
                    Configs,
                });
            case Action.Delete:
                foreach (var (zhou, variants) in Zhou)
                {
                    if (zhou.Variants.Count == variants.Count)
                    {
                        _context.DbContext.Zhous.Remove(zhou);
                    }
                    else
                    {
                        _context.DbContext.ZhouVariants.RemoveRange(variants);
                    }
                }
                await _context.DbContext.SaveChangesAsync();
                foreach (var (zhou, variants) in Zhou)
                {
                    if (zhou.Variants.Count == variants.Count)
                    {
                        imGuild.DeleteZhou(zhou.ZhouID);
                    }
                    else
                    {
                        foreach (var v in variants)
                        {
                            imGuild.DeleteZhouVariant(v.ZhouVariantID);
                        }
                    }
                }
                return RedirectToPage();
            default:
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostRefreshAsync()
        {
            var user = await CheckUserPrivilege();
            if (user is null)
            {
                return RedirectToPage("/Index");
            }

            var imGuild = await _context.GetGuildAsync(user.GuildID.Value);
            var imUser = imGuild.GetUserById(user.Id);

            var userAllConfigs = await _context.DbContext.UserCharacterConfigs
                .Include(c => c.CharacterConfig)
                .Where(c => c.UserID == user.Id)
                .ToListAsync();
            var userCharacters = userAllConfigs
                .Where(c => c.CharacterConfig.Kind == default)
                .Select(c => c.CharacterConfig.CharacterID)
                .ToHashSet();

            imUser.MatchAllZhouVariants(userCharacters, userAllConfigs.Select(c => c.CharacterConfigID).ToHashSet());
            imUser.LastComboCalculation = default;

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAllAsync()
        {
            var user = await CheckUserPrivilege();
            if (user is null || !IsOwner)
            {
                return RedirectToPage("/Index");
            }

            var imGuild = await _context.GetGuildAsync(user.GuildID.Value);
            var imUser = imGuild.GetUserById(user.Id);
            imGuild.DeleteAllZhous();

            _context.DbContext.Zhous.RemoveRange(_context.DbContext.Zhous.Where(z => z.GuildID == user.GuildID.Value));
            await _context.DbContext.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
