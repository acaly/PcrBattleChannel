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
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public DetailsModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
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
            var setting = await _context.UserZhouVariants
                .FirstOrDefaultAsync(uv => uv.UserID == UserID && uv.ZhouVariantID == v.ZhouVariantID);
            if (setting is null)
            {
                return (false, null);
            }
            return (true, setting.Borrow);
        }

        public async Task<(Character, ZhouVariantCharacterConfig[][])[]> GetConfigs(ZhouVariant v)
        {
            //Load necessary entries.
            await _context.Entry(v).Collection(v => v.CharacterConfigs).LoadAsync();
            foreach (var cc in v.CharacterConfigs)
            {
                await _context.Entry(cc).Reference(cc => cc.CharacterConfig).LoadAsync();
                await _context.Entry(cc.CharacterConfig).Reference(c => c.Character).LoadAsync();
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

            Zhou = await _context.Zhous
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

            var v = await _context.ZhouVariants
                .Include(v => v.Zhou)
                .FirstOrDefaultAsync(v => v.ZhouVariantID == vid);
            if (v is null || v.Zhou.GuildID != user.GuildID)
            {
                return NotFound();
            }

            var setting = await _context.UserZhouVariants
                .FirstOrDefaultAsync(uv => uv.UserID == user.Id && uv.ZhouVariantID == vid);

            //There is one case we don't need to remove.
            if (BorrowIndex.HasValue)
            {
                _context.UserCombos.RemoveRange(_context.UserCombos
                    .Where(c =>
                        c.Zhou1ID == setting.UserZhouVariantID ||
                        c.Zhou2ID == setting.UserZhouVariantID ||
                        c.Zhou3ID == setting.UserZhouVariantID));
            }

            if (BorrowIndex == -1)
            {
                if (setting is not null)
                {
                    _context.UserZhouVariants.Remove(setting);
                }
            }
            else
            {
                if (setting is null)
                {
                    setting = new UserZhouVariant
                    {
                        Borrow = BorrowIndex,
                        UserID = user.Id,
                        ZhouVariantID = vid.Value,
                    };
                    _context.UserZhouVariants.Add(setting);
                }
                else if (setting.Borrow != BorrowIndex)
                {
                    setting.Borrow = BorrowIndex;
                    _context.UserZhouVariants.Update(setting);
                }
            }
            await _context.SaveChangesAsync();

            return StatusCode(200);
        }
    }
}
