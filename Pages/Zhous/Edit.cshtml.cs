using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Zhous
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public EditModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [BindProperty]
        public Zhou Zhou { get; set; }

        [BindProperty]
        [Required]
        [Display(Name = "轴名")]
        public string Zhou_Name { get; set; }

        [BindProperty]
        public ZhouVariant EditVariant { get; set; }

        [BindProperty]
        public string EditVariantConfigs { get; set; }

        //Used in partial view. Properties names are the same with the page model so
        //they can be bound properly in partial view's Ajax requests.
        public class EditPartialModel
        {
            [BindProperty]
            public Zhou Zhou { get; set; }

            [BindProperty]
            public ZhouVariant EditVariant { get; set; }

            [BindProperty]
            public string EditVariantConfigs { get; set; }

            public HashSet<int> SelectedConfigIDs;

            public bool IsNew => EditVariant.ZhouVariantID == 0;
        }

        public EditPartialModel NewVariantModel() => new()
        {
            Zhou = Zhou,
            EditVariant = new(),
            SelectedConfigIDs = new(),
            EditVariantConfigs = "",
        };
        public async Task<EditPartialModel> VariantModel(ZhouVariant v)
        {
            await _context.Entry(v).Collection(vv => vv.CharacterConfigs).LoadAsync();
            var ids = v.CharacterConfigs
                .Where(cc => cc.CharacterConfigID.HasValue)
                .Select(cc => cc.CharacterConfigID.Value).ToHashSet();
            return new()
            {
                Zhou = Zhou,
                EditVariant = v,
                SelectedConfigIDs = ids,
                EditVariantConfigs = string.Join(',', ids),
            };
        }

        private async Task<List<ZhouVariantCharacterConfig>> ApplyConfigString(Zhou z, ZhouVariant v, string str)
        {
            try
            {
                //Prepare a list to help calculate CharacterIndex field.
                var cidList = new List<int>();
                cidList.Add(z.C1ID ?? -1);
                cidList.Add(z.C2ID ?? -1);
                cidList.Add(z.C3ID ?? -1);
                cidList.Add(z.C4ID ?? -1);
                cidList.Add(z.C5ID ?? -1);
                cidList.RemoveAll(ii => ii == -1);

                //Collect info of all configs.
                var selectedIds = str?.Split(',').Select(i => int.Parse(i)) ?? Enumerable.Empty<int>();
                Dictionary<CharacterConfigKind, List<CharacterConfig>> dict = new();
                foreach (var ccid in selectedIds)
                {
                    var cc = await _context.CharacterConfigs.FirstOrDefaultAsync(cc => cc.CharacterConfigID == ccid);
                    if (!dict.TryGetValue(cc.Kind, out var list))
                    {
                        list = new();
                        dict.Add(cc.Kind, list);
                    }
                    list.Add(cc);
                }

                //Add new configs (collect before submitting to context).
                List<ZhouVariantCharacterConfig> newInfoList = new();
                foreach (var (kind, list) in dict)
                {
                    foreach (var cc in list)
                    {
                        var cid = cidList.IndexOf(cc.CharacterID);
                        if (cid == -1) return null; //The character is not in this Zhou.
                        var ccInfo = new ZhouVariantCharacterConfig()
                        {
                            ZhouVariant = v,
                            CharacterConfigID = cc.CharacterConfigID,
                            CharacterIndex = cid,
                            OrGroupIndex = (int)kind,
                        };
                        newInfoList.Add(ccInfo);
                    }
                }

                //Remove old configs.
                if (v.ZhouVariantID != 0)
                {
                    await _context.Entry(v).Collection(v => v.CharacterConfigs).LoadAsync();
                    v.CharacterConfigs.Clear();
                }

                //Add new configs.
                foreach (var info in newInfoList)
                {
                    _context.ZhouVariantCharacterConfigs.Add(info);
                }

                return newInfoList;
            }
            catch
            {
                return null;
            }
        }

        private async Task<PcrIdentityUser> CheckUserPrivilege()
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
            var guild = await _context.Guilds.FirstOrDefaultAsync(g => g.GuildID == user.GuildID);
            if (guild is null)
            {
                return null;
            }
            return user;
        }

        private async Task<CharacterConfig[][]> GetConfigForCharacter(int guildID, int characterID)
        {
            var allConfigs = await _context.CharacterConfigs
                .Where(cc => cc.GuildID == guildID && cc.CharacterID == characterID)
                .ToListAsync();
            return allConfigs.GroupBy(c => c.Kind).Select(g => g.ToArray()).ToArray();
        }

        private async Task<List<(string, CharacterConfig[][])>> InitCharacterConfigs(int guildID)
        {
            var ret = new List<(string, CharacterConfig[][])>();
            if (!Zhou.C1ID.HasValue)
            {
                return ret;
            }
            ret.Add((Zhou.C1.Name, await GetConfigForCharacter(guildID, Zhou.C1ID.Value)));
            if (!Zhou.C2ID.HasValue)
            {
                return ret;
            }
            ret.Add((Zhou.C2.Name, await GetConfigForCharacter(guildID, Zhou.C2ID.Value)));
            if (!Zhou.C3ID.HasValue)
            {
                return ret;
            }
            ret.Add((Zhou.C3.Name, await GetConfigForCharacter(guildID, Zhou.C3ID.Value)));
            if (!Zhou.C4ID.HasValue)
            {
                return ret;
            }
            ret.Add((Zhou.C4.Name, await GetConfigForCharacter(guildID, Zhou.C4ID.Value)));
            if (!Zhou.C5ID.HasValue)
            {
                return ret;
            }
            ret.Add((Zhou.C5.Name, await GetConfigForCharacter(guildID, Zhou.C5ID.Value)));
            return ret;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var user = await CheckUserPrivilege();
            if (user is null)
            {
                return RedirectToPage("/Index");
            }

            if (id == null)
            {
                return NotFound();
            }

            Zhou = await _context.Zhous
                .Include(z => z.Boss)
                .Include(z => z.C1)
                .Include(z => z.C2)
                .Include(z => z.C3)
                .Include(z => z.C4)
                .Include(z => z.C5)
                .Include(z => z.Variants)
                .FirstOrDefaultAsync(m => m.ZhouID == id);
            Zhou_Name = Zhou.Name;

            if (Zhou == null)
            {
                return NotFound();
            }

            ViewData["allConfigs"] = await InitCharacterConfigs(user.GuildID.Value);

            ViewData["BossID"] = new SelectList(_context.Bosses, "BossID", "ShortName");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await CheckUserPrivilege();
            if (user is null)
            {
                return RedirectToPage("/Index");
            }

            if (!ModelState.IsValid)
            {
                return RedirectToPage();
            }

            var zhou = await _context.Zhous.FirstOrDefaultAsync(z => z.ZhouID == Zhou.ZhouID);
            if (zhou is null || zhou.GuildID != user.GuildID)
            {
                return NotFound();
            }

            zhou.Name = Zhou_Name;
            zhou.Description = Zhou.Description;
            zhou.BossID = Zhou.BossID;

            _context.Update(zhou);

            await _context.SaveChangesAsync();
            return RedirectToPage("./Details", new { id = Zhou.ZhouID });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int? id)
        {
            var user = await CheckUserPrivilege();
            if (user is null)
            {
                return RedirectToPage("./Index");
            }
            if (!id.HasValue)
            {
                return RedirectToPage("./Index");
            }

            var zhou = await _context.Zhous
                .Include(z => z.Variants)
                .FirstOrDefaultAsync(z => z.ZhouID == id);
            if (zhou is null || zhou.GuildID != user.GuildID)
            {
                return NotFound();
            }
            foreach (var v in zhou.Variants)
            {
                await CheckAndRemoveUserVariantsAsync(_context, id.Value);
            }
            zhou.Variants.Clear();
            _context.Zhous.Remove(zhou);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        //This method is also used by ImportModel.
        public static async Task CheckAndAddUserVariants(ApplicationDbContext context, int guildID,
            Zhou zhou, ZhouVariant variant, IEnumerable<ZhouVariantCharacterConfig> configs, List<UserCharacterConfig> newUserConfigs)
        {
            var availableUsers = new HashSet<string>();
            var tempSet = new HashSet<string>();
            var userBorrow = new Dictionary<string, int>();
            var deleteList = new List<string>();

            void Merge(IEnumerable<string> list, int borrow)
            {
                HashSet<string> mergeSet;
                if (list is HashSet<string> s)
                {
                    mergeSet = s;
                }
                else
                {
                    tempSet.Clear();
                    tempSet.UnionWith(list);
                    mergeSet = tempSet;
                }

                deleteList.Clear();
                foreach (var (u, b) in userBorrow)
                {
                    if (!mergeSet.Contains(u))
                    {
                        deleteList.Add(u);
                    }
                }
                foreach (var u in deleteList)
                {
                    userBorrow.Remove(u);
                }

                deleteList.Clear();
                foreach (var u in availableUsers)
                {
                    if (!mergeSet.Contains(u))
                    {
                        userBorrow.Add(u, borrow);
                        deleteList.Add(u);
                    }
                }
                foreach (var u in deleteList)
                {
                    availableUsers.Remove(u);
                }
            }

            //Mark all users as available.
            availableUsers.UnionWith(await context.Users
                .Where(u => u.GuildID == guildID)
                .Select(u => u.Id)
                .ToListAsync());

            //Check characters (default config).
            async Task<List<string>> FilterCharacter(int? characterID)
            {
                if (!characterID.HasValue) return new();
                var defaultConfig = await context.CharacterConfigs
                    .FirstOrDefaultAsync(c => c.GuildID == guildID && c.CharacterID == characterID && c.Kind == default);
                if (defaultConfig is null)
                {
                    availableUsers.Clear();
                    return new();
                }
                return await context.UserCharacterConfigs
                    .Where(c => c.CharacterConfigID == defaultConfig.CharacterConfigID)
                    .Select(c => c.UserID)
                    .ToListAsync();
            }
            Merge(await FilterCharacter(zhou.C1ID), 0);
            Merge(await FilterCharacter(zhou.C2ID), 1);
            Merge(await FilterCharacter(zhou.C3ID), 2);
            Merge(await FilterCharacter(zhou.C4ID), 3);
            Merge(await FilterCharacter(zhou.C5ID), 4);

            //Check additional configs.
            var orGroupUsers = new HashSet<string>();
            foreach (var configGroup in configs.GroupBy(c => (c.CharacterIndex, c.OrGroupIndex)))
            {
                foreach (var c in configGroup)
                {
                    if (c.CharacterConfigID.HasValue)
                    {
                        orGroupUsers.UnionWith(await context.UserCharacterConfigs
                            .Where(cc => cc.CharacterConfigID == c.CharacterConfigID)
                            .Select(u => u.UserID)
                            .ToListAsync());
                    }
                    else
                    {
                        orGroupUsers.UnionWith(newUserConfigs
                            .Where(cc => cc.CharacterConfig == c.CharacterConfig)
                            .Select(u => u.UserID));
                    }
                }
                Merge(orGroupUsers, configGroup.Key.CharacterIndex);
            }

            //Add the variant to available users.
            foreach (var uid in availableUsers)
            {
                context.UserZhouVariants.Add(new UserZhouVariant
                {
                    UserID = uid,
                    ZhouVariant = variant,
                    Borrow = null,
                });
            }
            foreach (var (uid, borrow) in userBorrow)
            {
                context.UserZhouVariants.Add(new UserZhouVariant
                {
                    UserID = uid,
                    ZhouVariant = variant,
                    Borrow = borrow,
                });
            }
        }

        public static async Task CheckAndRemoveUserVariantsAsync(ApplicationDbContext context, int zhouVariantID)
        {
            var uzvs = await context.UserZhouVariants
                .Where(uzv => uzv.ZhouVariantID == zhouVariantID)
                .ToListAsync();
            foreach (var uzv in uzvs)
            {
                context.UserCombos.RemoveRange(context.UserCombos
                    .Where(c =>
                        c.Zhou1ID == uzv.UserZhouVariantID ||
                        c.Zhou2ID == uzv.UserZhouVariantID ||
                        c.Zhou3ID == uzv.UserZhouVariantID));
                context.UserZhouVariants.Remove(uzv);
            }
        }

        public static void CheckAndRemoveAll(ApplicationDbContext context, int guildID)
        {
            context.UserCombos.RemoveRange(context.UserCombos.Where(c => c.GuildID == guildID));
            context.Zhous.RemoveRange(context.Zhous.Where(z => z.GuildID == guildID));
        }

        //Ajax
        public async Task<IActionResult> OnPostAddvAsync()
        {
            var user = await CheckUserPrivilege();
            if (user is null)
            {
                return StatusCode(400);
            }

            //Need info of Zhou to render partial view.
            Zhou = await _context.Zhous
                .Include(z => z.Boss)
                .Include(z => z.C1)
                .Include(z => z.C2)
                .Include(z => z.C3)
                .Include(z => z.C4)
                .Include(z => z.C5)
                .Include(z => z.Variants)
                .FirstOrDefaultAsync(z => z.ZhouID == Zhou.ZhouID);
            if (Zhou is null)
            {
                return NotFound();
            }

            var variant = new ZhouVariant
            {
                ZhouID = Zhou.ZhouID,
                Name = EditVariant.Name,
                Content = EditVariant.Content,
                IsDraft = EditVariant.IsDraft,
                Damage = EditVariant.Damage,
            };

            _context.ZhouVariants.Add(variant);
            var configs = await ApplyConfigString(Zhou, variant, EditVariantConfigs);

            //Setup user variants.
            await CheckAndAddUserVariants(_context, user.GuildID.Value, Zhou, variant, configs, null);

            await _context.SaveChangesAsync();

            ViewData["allConfigs"] = await InitCharacterConfigs(user.GuildID.Value);
            return Partial("_Edit_VariantPartial", await VariantModel(variant));
        }

        //Ajax
        public async Task<IActionResult> OnPostDeletevAsync(int? id)
        {
            var user = await CheckUserPrivilege();
            if (user is null || !id.HasValue)
            {
                return StatusCode(400);
            }

            var v = await _context.ZhouVariants
                .Include(v => v.Zhou)
                .FirstOrDefaultAsync(v => v.ZhouVariantID == id.Value);
            if (v is null)
            {
                return NotFound();
            }
            if (v.Zhou.GuildID != user.GuildID)
            {
                return StatusCode(400);
            }

            await CheckAndRemoveUserVariantsAsync(_context, id.Value);
            _context.ZhouVariants.Remove(v);
            await _context.SaveChangesAsync();

            return StatusCode(200);
        }

        //Ajax
        public async Task<IActionResult> OnPostEditvAsync(int? id)
        {
            var user = await CheckUserPrivilege();
            if (user is null)
            {
                return StatusCode(400);
            }

            var v = await _context.ZhouVariants
                .Include(v => v.Zhou)
                .FirstOrDefaultAsync(v => v.ZhouVariantID == id);
            if (v is null)
            {
                return NotFound();
            }
            if (v.Zhou.GuildID != user.GuildID)
            {
                return StatusCode(400);
            }

            //Need info of Zhou to render partial view.
            Zhou = await _context.Zhous
                .Include(z => z.Boss)
                .Include(z => z.C1)
                .Include(z => z.C2)
                .Include(z => z.C3)
                .Include(z => z.C4)
                .Include(z => z.C5)
                .Include(z => z.Variants)
                .FirstOrDefaultAsync(z => z.ZhouID == Zhou.ZhouID);
            if (Zhou is null)
            {
                return NotFound();
            }

            v.Name = EditVariant.Name;
            v.Content = EditVariant.Content;
            v.IsDraft = EditVariant.IsDraft;
            v.Damage = EditVariant.Damage;

            _context.Update(v);
            await ApplyConfigString(Zhou, v, EditVariantConfigs);
            await _context.SaveChangesAsync();

            ViewData["allConfigs"] = await InitCharacterConfigs(user.GuildID.Value);
            return Partial("_Edit_VariantPartial", await VariantModel(v));
        }

        public override PartialViewResult Partial(string viewName, object model)
        {
            ViewDataDictionary viewData = new ViewDataDictionary(MetadataProvider, ViewData.ModelState)
            {
                Model = model,
            };
            viewData.Add("allConfigs", ViewData["allConfigs"]);
            return new PartialViewResult
            {
                ViewName = viewName,
                ViewData = viewData,
            };
        }
    }
}
