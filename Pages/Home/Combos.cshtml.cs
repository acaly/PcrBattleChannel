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
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public CombosModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public List<UserCombo> UserCombo { get; set; }
        public PcrIdentityUser AppUser { get; set; }
        public List<UserCharacterStatus> UsedCharacters { get; set; }
        public HashSet<int> UsedCharacterIds { get; set; }

        public List<Character> AllCharacters { get; set; }

        [BindProperty]
        [Display(Name = "选择已用角色（不含助战）")]
        public string UsedCharacterString { get; set; }

        private async Task GetUserInfo(PcrIdentityUser user)
        {
            AppUser = user;

            if (user.Attempt1ID.HasValue)
            {
                await _context.Entry(user).Reference(u => u.Attempt1).LoadAsync();
                await _context.Entry(user.Attempt1).Reference(uv => uv.ZhouVariant).LoadAsync();
                await _context.Entry(user.Attempt1.ZhouVariant).Reference(v => v.Zhou).LoadAsync();
                await _context.Entry(user.Attempt1.ZhouVariant.Zhou).Reference(z => z.Boss).LoadAsync();
            }
            if (user.Attempt2ID.HasValue)
            {
                await _context.Entry(user).Reference(u => u.Attempt2).LoadAsync();
                await _context.Entry(user.Attempt2).Reference(uv => uv.ZhouVariant).LoadAsync();
                await _context.Entry(user.Attempt2.ZhouVariant).Reference(v => v.Zhou).LoadAsync();
                await _context.Entry(user.Attempt2.ZhouVariant.Zhou).Reference(z => z.Boss).LoadAsync();
            }
            if (user.Attempt3ID.HasValue)
            {
                await _context.Entry(user).Reference(u => u.Attempt3).LoadAsync();
                await _context.Entry(user.Attempt3).Reference(uv => uv.ZhouVariant).LoadAsync();
                await _context.Entry(user.Attempt3.ZhouVariant).Reference(v => v.Zhou).LoadAsync();
                await _context.Entry(user.Attempt3.ZhouVariant.Zhou).Reference(z => z.Boss).LoadAsync();
            }
        }

        private async Task GetAllCharacters(PcrIdentityUser user)
        {
            var allZhous = _context.Zhous
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
                if (!await _context.UserCharacterConfigs
                    .Include(cc => cc.CharacterConfig)
                    .AnyAsync(cc => cc.UserID == user.Id && cc.CharacterConfig.CharacterID == c))
                {
                    continue;
                }
                AllCharacters.Add(await _context.Characters.FirstOrDefaultAsync(cc => cc.CharacterID == c));
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
            if (!user.GuildID.HasValue)
            {
                return Redirect("/");
            }
            await GetUserInfo(user);

            UserCombo = await _context.UserCombos
                .Include(u => u.Zhou1)
                .Include(u => u.Zhou2)
                .Include(u => u.Zhou3)
                .Where(u => u.UserID == user.Id)
                .OrderByDescending(u => u.NetValue)
                .ToListAsync();
            foreach (var c in UserCombo)
            {
                if (c.Zhou1ID.HasValue)
                {
                    await _context.Entry(c.Zhou1).Reference(z => z.ZhouVariant).LoadAsync();
                    await _context.Entry(c.Zhou1.ZhouVariant).Reference(v => v.Zhou).LoadAsync();
                    await _context.Entry(c.Zhou1.ZhouVariant.Zhou).Reference(z => z.Boss).LoadAsync();
                }
                if (c.Zhou2ID.HasValue)
                {
                    await _context.Entry(c.Zhou2).Reference(z => z.ZhouVariant).LoadAsync();
                    await _context.Entry(c.Zhou2.ZhouVariant).Reference(v => v.Zhou).LoadAsync();
                    await _context.Entry(c.Zhou2.ZhouVariant.Zhou).Reference(z => z.Boss).LoadAsync();
                }
                if (c.Zhou3ID.HasValue)
                {
                    await _context.Entry(c.Zhou3).Reference(z => z.ZhouVariant).LoadAsync();
                    await _context.Entry(c.Zhou3.ZhouVariant).Reference(v => v.Zhou).LoadAsync();
                    await _context.Entry(c.Zhou3.ZhouVariant.Zhou).Reference(z => z.Boss).LoadAsync();
                }
            }

            UsedCharacters = await _context.UserCharacterStatuses
                .Include(s => s.Character)
                .Where(s => s.UserID == user.Id && s.IsUsed == true)
                .ToListAsync();
            UsedCharacters.Sort((a, b) => Math.Sign(a.Character.Range - b.Character.Range));
            UsedCharacterIds = UsedCharacters.Select(c => c.CharacterID).ToHashSet();

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
            if (!user.GuildID.HasValue)
            {
                return Redirect("/");
            }

            await _context.UserCombos.Where(u => u.UserID == user.Id).DeleteFromQueryAsync();
            await FindAllCombos.Run(_context, user);
            _context.SaveChanges();

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
            if (!user.GuildID.HasValue)
            {
                return StatusCode(400);
            }

            user = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            user.GuessedAttempts = 0;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

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
            if (!user.GuildID.HasValue)
            {
                return StatusCode(400);
            }
            await GetUserInfo(user);

            await _context.UserCharacterStatuses
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
                        Character = await _context.Characters.FirstAsync(c => c.CharacterID == cid),
                        CharacterID = cid,
                        IsUsed = true,
                    };
                    UsedCharacters.Add(s);
                    _context.UserCharacterStatuses.Add(s);
                }
                user.Attempts = list.Length switch
                {
                    > 10 => 3,
                    > 5 => 2,
                    > 0 => 1,
                    _ => 0,
                };
                user.Attempt1ID = null;
                user.Attempt2ID = null;
                user.Attempt3ID = null;
                user.GuessedAttempts = 0;
            }
            catch
            {
                return StatusCode(400);
            }
            await _context.SaveChangesAsync();

            await GetAllCharacters(user);

            UsedCharacters.Sort((a, b) => Math.Sign(a.Character.Range - b.Character.Range));
            UsedCharacterIds = UsedCharacters.Select(c => c.CharacterID).ToHashSet();

            return Partial("_Combo_StatusPartial", this);
        }
    }
}
