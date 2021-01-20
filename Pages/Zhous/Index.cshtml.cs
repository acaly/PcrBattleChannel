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
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public IndexModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public bool IsAdmin { get; set; }
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
            if (!user.GuildID.HasValue)
            {
                return null;
            }
            Guild = await _context.Guilds.FirstOrDefaultAsync(g => g.GuildID == user.GuildID);
            if (Guild is null)
            {
                return null;
            }
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

            Zhou = await _context.Zhous
                .Include(z => z.Boss)
                .Include(z => z.C1)
                .Include(z => z.C2)
                .Include(z => z.C3)
                .Include(z => z.C4)
                .Include(z => z.C5)
                .Include(z => z.Guild)
                .Where(z => z.GuildID == user.GuildID).ToListAsync();

            var allSettings = await _context.UserZhouVariants
                .Include(uv => uv.ZhouVariant)
                .Where(uv => uv.UserID == user.Id)
                .ToListAsync();
            UserZhouSettings = allSettings
                .GroupBy(uv => uv.ZhouVariant.ZhouID)
                .Select(g => g.Key).ToHashSet();

            return Page();
        }
    }
}
