using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Guilds
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public EditModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public Guild Guild { get; set; }

        [BindProperty]
        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        public string InviteMemberEmail { get; set; }

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

        public async Task<IActionResult> OnGetAsync()
        {
            Guild = await CheckUserPrivilege();
            if (Guild is null)
            {
                return RedirectToPage("/Home/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var guild = await CheckUserPrivilege();
            if (guild is null)
            {
                return RedirectToPage();
            }
            if (!ModelState.IsValid || Guild.GuildID != guild.GuildID)
            {
                return Page();
            }

            guild.Name = Guild.Name;
            guild.Description = Guild.Description;
            guild.YobotAPI = Guild.YobotAPI;

            _context.Guilds.Update(guild);
            await _context.SaveChangesAsync();

            StatusMessage = "公会信息已更新。";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostInviteAsync()
        {
            var guild = await CheckUserPrivilege();
            if (guild is null)
            {
                return RedirectToPage("/Home/Index");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == InviteMemberEmail);
            if (user is null)
            {
                StatusMessage = "错误：用户不存在。";
                return RedirectToPage();
            }
            if (user.GuildID.HasValue)
            {
                StatusMessage = "错误：用户已经所属其他公会。";
                return RedirectToPage();
            }

            user.GuildID = guild.GuildID;
            user.IsGuildAdmin = false;
            _context.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAdminAsync(string id)
        {
            var guild = await CheckUserPrivilege();
            if (guild is null)
            {
                return RedirectToPage("/Home/Index");
            }

            var user = await _context.Users.FindAsync(id);
            if (user is null || user.GuildID != guild.GuildID)
            {
                StatusMessage = "错误：用户不存在。";
                return RedirectToPage();
            }

            user.IsGuildAdmin = !user.IsGuildAdmin;
            _context.Update(user);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            var guild = await CheckUserPrivilege();
            if (guild is null)
            {
                return RedirectToPage("/Home/Index");
            }

            var user = await _context.Users.FindAsync(id);
            if (user is null || user.GuildID != guild.GuildID)
            {
                StatusMessage = "错误：用户不存在。";
                return RedirectToPage();
            }

            user.GuildID = null;
            user.IsGuildAdmin = false;

            user.Attempts = 0;
            user.GuessedAttempts = 0;
            user.Attempt1ID = user.Attempt2ID = user.Attempt3ID = null;
            user.Attempt1Borrow = user.Attempt2Borrow = user.Attempt3Borrow = null;
            user.IsIgnored = false;

            _context.Users.Update(user);

            await _context.UserCombos.Where(c => c.UserID == user.Id).DeleteFromQueryAsync();
            await _context.UserZhouVariants.Where(c => c.UserID == user.Id).DeleteFromQueryAsync();
            await _context.UserCharacterConfigs.Where(c => c.UserID == user.Id).DeleteFromQueryAsync();
            await _context.UserCharacterStatuses.Where(c => c.UserID == user.Id).DeleteFromQueryAsync();

            if (user.Email is null)
            {
                //Cloned user has no email.
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();

            StatusMessage = "用户已移出公会。";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLoginAsync(string id)
        {
            var guild = await CheckUserPrivilege();
            if (guild is null)
            {
                return RedirectToPage("/Home/Index");
            }

            var user = await _context.Users.FindAsync(id);
            if (user is null || user.GuildID != guild.GuildID)
            {
                StatusMessage = "错误：用户不存在。";
                return RedirectToPage();
            }

            await _signInManager.SignInAsync(user, false);
            return RedirectToPage("/Home/Index");
        }
    }
}
