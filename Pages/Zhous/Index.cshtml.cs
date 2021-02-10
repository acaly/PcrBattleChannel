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
        public List<Zhou> Zhou { get; set; }
        public HashSet<int> UserZhouSettings { get; set; }

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

            //Are we using too many includes?
            Zhou = await _context.DbContext.Zhous
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

            UserZhouSettings = imGuild.ZhouVariants
                .Where(zv => zv.UserData[imUser.Index].BorrowPlusOne != 0)
                .Select(zv => zv.ZhouID)
                .ToHashSet();

            return Page();
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
