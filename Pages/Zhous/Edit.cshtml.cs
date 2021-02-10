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
        private readonly InMemoryStorageContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public EditModel(InMemoryStorageContext context, SignInManager<PcrIdentityUser> signInManager,
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
            await _context.DbContext.Entry(v).Collection(vv => vv.CharacterConfigs).LoadAsync();
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
                    var cc = await _context.DbContext.CharacterConfigs.FirstOrDefaultAsync(cc => cc.CharacterConfigID == ccid);
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
                    await _context.DbContext.Entry(v).Collection(v => v.CharacterConfigs).LoadAsync();
                    v.CharacterConfigs.Clear();
                }

                //Add new configs.
                foreach (var info in newInfoList)
                {
                    _context.DbContext.ZhouVariantCharacterConfigs.Add(info);
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
            user = await _context.DbContext.Users
                .Include(u => u.Guild)
                .FirstOrDefaultAsync(uu => uu.Id == user.Id);
            if (user.Guild is null)
            {
                return null;
            }
            return user;
        }

        private async Task<CharacterConfig[][]> GetConfigForCharacter(int guildID, int characterID)
        {
            var allConfigs = await _context.DbContext.CharacterConfigs
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

            Zhou = await _context.DbContext.Zhous
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

            var zhou = await _context.DbContext.Zhous.FirstOrDefaultAsync(z => z.ZhouID == Zhou.ZhouID);
            if (zhou is null || zhou.GuildID != user.GuildID)
            {
                return NotFound();
            }

            zhou.Name = Zhou_Name;
            zhou.Description = Zhou.Description;

            _context.DbContext.Update(zhou);

            await _context.DbContext.SaveChangesAsync();
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
            var imGuild = await _context.GetGuildAsync(user.GuildID.Value);

            //Remove zhou variants.
            //If these are loaded through zhou.Variants, the db refuses to delete them.
            //Not sure what causes this, but here removing them without loading from zhou works fine.
            foreach (var v in await _context.DbContext.ZhouVariants.Where(zv => zv.ZhouID == id.Value).ToListAsync())
            {
                _context.DbContext.ZhouVariants.Remove(v);
            }

            //Then remove the zhou.
            var zhou = await _context.DbContext.Zhous
                .FirstOrDefaultAsync(z => z.ZhouID == id);
            if (zhou is null || zhou.GuildID != user.GuildID)
            {
                return RedirectToPage("./Index");
            }

            _context.DbContext.Zhous.Remove(zhou);

            await _context.DbContext.SaveChangesAsync();
            imGuild.DeleteZhou(zhou.ZhouID);

            return RedirectToPage("./Index");
        }

        //Ajax
        public async Task<IActionResult> OnPostAddvAsync()
        {
            var user = await CheckUserPrivilege();
            if (user is null)
            {
                return StatusCode(400);
            }
            var imGuild = await _context.GetGuildAsync(user.GuildID.Value);

            //Need info of Zhou to render partial view.
            Zhou = await _context.DbContext.Zhous
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
            var allUserConfigs = await _context.DbContext.UserCharacterConfigs
                .Include(ucc => ucc.CharacterConfig)
                .Where(ucc => ucc.CharacterConfig.GuildID == user.GuildID.Value)
                .ToListAsync();

            _context.DbContext.ZhouVariants.Add(variant);
            var configs = await ApplyConfigString(Zhou, variant, EditVariantConfigs);

            //Setup user variants.
            imGuild.AddZhouVariant(allUserConfigs, variant, Zhou, configs);

            await _context.DbContext.SaveChangesAsync();

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

            var v = await _context.DbContext.ZhouVariants
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
            var imGuild = await _context.GetGuildAsync(user.GuildID.Value);

            _context.DbContext.ZhouVariants.Remove(v);

            await _context.DbContext.SaveChangesAsync();
            imGuild.DeleteZhouVariant(v.ZhouVariantID);

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

            var v = await _context.DbContext.ZhouVariants
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
            var imGuild = await _context.GetGuildAsync(user.GuildID.Value);

            //Need info of Zhou to render partial view.
            Zhou = await _context.DbContext.Zhous
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

            _context.DbContext.Update(v);
            await ApplyConfigString(Zhou, v, EditVariantConfigs);

            await _context.DbContext.SaveChangesAsync();
            imGuild.UpdateZhouVariant(v);

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
