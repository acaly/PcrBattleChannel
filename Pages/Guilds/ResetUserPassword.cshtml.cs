using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Guilds
{
    public class ResetUserPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public ResetUserPasswordModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public string Code { get; private set; }

        private async Task<Guild> CheckUserPrivilege()
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
            var guild = await _context.Guilds
                .Include(g => g.Owner)
                .Include(g => g.Members)
                .FirstOrDefaultAsync(m => m.GuildID == user.GuildID.Value);
            if (guild.OwnerID != user.Id)
            {
                return null;
            }
            return guild;
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (id is null)
            {
                return BadRequest();
            }
            var guild = await CheckUserPrivilege();
            if (guild is null)
            {
                return BadRequest();
            }
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (targetUser is null || targetUser.GuildID != guild.GuildID)
            {
                return BadRequest();
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(targetUser);
            Code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            return Page();
        }
    }
}
