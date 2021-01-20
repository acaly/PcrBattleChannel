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

        public List<UserCombo> UserCombo { get;set; }

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

            UserCombo = await _context.UserCombos
                .Include(u => u.Zhou1)
                .Include(u => u.Zhou2)
                .Include(u => u.Zhou3).ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostRecalcAsync()
        {
            //collect all variants
            //merge by Zhou+BorrowIndex, select highest
            //calculate most-used 4 characters, put variants with all 3 in a special group A
            //  at most only one of these can be chosen
            //put variants with 1st, 2nd, 4th most-used characters in another special group B
            //  at most only one of these can be chosen
            //put all others (without these 4) in another group C
            //iterate and generate results
            //  A (1) + B (1) + C (1)
            //  A (1) + C (2)
            //  B (1) + C (2)
            //  C (3)

            return StatusCode(200);
        }
    }
}
