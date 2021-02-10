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
    public class DetailsModel : PageModel
    {
        private readonly InMemoryStorageContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public DetailsModel(InMemoryStorageContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public bool IsAdmin { get; set; }
        public string UserID { get; set; }
        public Zhou Zhou { get; set; }

        [BindProperty]
        public int? BorrowIndex { get; set; }

        public async Task<(bool enabled, int? borrow)> GetBorrowSetting(ZhouVariant v)
        {
            var imGuild = await _context.GetGuildAsync(v.Zhou.GuildID);
            var imUser = imGuild.GetUserById(UserID);
            var borrowPlusOne = imGuild.GetZhouVariantById(v.ZhouVariantID).UserData[imUser.Index].BorrowPlusOne;
            return borrowPlusOne switch
            {
                >= 1 and <= 5 => (true, borrowPlusOne - 1),
                6 => (true, null),
                _ => (false, null),
            };
        }

        public async Task<(Character, ZhouVariantCharacterConfig[][])[]> GetConfigs(ZhouVariant v)
        {
            //Load necessary entries.
            await _context.DbContext.Entry(v).Collection(v => v.CharacterConfigs).LoadAsync();
            foreach (var cc in v.CharacterConfigs)
            {
                await _context.DbContext.Entry(cc).Reference(cc => cc.CharacterConfig).LoadAsync();
                await _context.DbContext.Entry(cc.CharacterConfig).Reference(c => c.Character).LoadAsync();
            }

            //Group.
            var configs = v.CharacterConfigs
                .GroupBy(cc => cc.CharacterIndex)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.First().CharacterIndex,
                    g => g.GroupBy(cc => cc.OrGroupIndex).Select(gg => gg.ToArray()).ToArray());

            //Add default config if not existing.
            var ret = new (Character character, ZhouVariantCharacterConfig[][] configs)[5];
            ret[0].character = v.Zhou.C1;
            ret[1].character = v.Zhou.C2;
            ret[2].character = v.Zhou.C3;
            ret[3].character = v.Zhou.C4;
            ret[4].character = v.Zhou.C5;
            for (int i = 0; i < 5; ++i)
            {
                if (configs.TryGetValue(i, out var t))
                {
                    ret[i].configs = t;
                }
            }
            return ret;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            if (!_signInManager.IsSignedIn(User))
            {
                return NotFound();
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue)
            {
                return NotFound();
            }
            UserID = user.Id;
            IsAdmin = user.IsGuildAdmin;

            Zhou = await _context.DbContext.Zhous
                .Include(z => z.Boss)
                .Include(z => z.C1)
                .Include(z => z.C2)
                .Include(z => z.C3)
                .Include(z => z.C4)
                .Include(z => z.C5)
                .Include(z => z.Variants)
                .FirstOrDefaultAsync(m => m.ZhouID == id);

            if (Zhou == null || Zhou.GuildID != user.GuildID)
            {
                return NotFound();
            }

            return Page();
        }

        //Ajax
        public async Task<IActionResult> OnPostBorrowAsync(int? vid)
        {
            if (vid == null)
            {
                return NotFound();
            }
            if (!_signInManager.IsSignedIn(User))
            {
                return NotFound();
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue)
            {
                return NotFound();
            }

            var v = await _context.DbContext.ZhouVariants
                .Include(v => v.Zhou)
                .FirstOrDefaultAsync(v => v.ZhouVariantID == vid);
            if (v is null || v.Zhou.GuildID != user.GuildID)
            {
                return NotFound();
            }

            var imGuild = await _context.GetGuildAsync(user.GuildID.Value);
            var imUser = imGuild.GetUserById(user.Id);
            var imZV = imGuild.GetZhouVariantById(vid.Value);

            imZV.UserData[imUser.Index].BorrowPlusOne = BorrowIndex switch
            {
                null => 6,
                >= 0 and <= 4 => (byte)(BorrowIndex.Value + 1),
                _ => 0,
            };

            imUser.LastComboCalculation = default; //Force recalculation.
            //No need to save db context.

            return StatusCode(200);
        }
    }
}
