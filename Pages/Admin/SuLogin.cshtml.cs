using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class SuLoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;

        public SuLoginModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager)
        {
            _context = context;
            _signInManager = signInManager;
        }

        [BindProperty]
        public ulong QQ { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var u = await _context.Users.FirstOrDefaultAsync(u => u.QQID == QQ);
            await _signInManager.SignInAsync(u, false);
            return RedirectToPage("/Home/Index");
        }
    }
}
