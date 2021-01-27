using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Algorithm;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Guilds
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

        public Guild Guild { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToPage("/Home/Index");
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue)
            {
                return RedirectToPage("/Home/Index");
            }

            Guild = await _context.Guilds
                .Include(g => g.Owner)
                .Include(g => g.Members)
                .FirstOrDefaultAsync(m => m.GuildID == user.GuildID.Value);

            if (Guild == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSyncAsync()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToPage("/Home/Index");
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue || !user.IsGuildAdmin)
            {
                return RedirectToPage("/Home/Index");
            }

            var g = await _context.Guilds
                .FirstOrDefaultAsync(m => m.GuildID == user.GuildID.Value);

            await YobotSync.RunSingleAsync(_context, g);
            return RedirectToPage("/Home/Index");
        }
    }
}
