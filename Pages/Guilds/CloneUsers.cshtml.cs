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

namespace PcrBattleChannel.Pages.Guilds
{
    public class CloneUsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public CloneUsersModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        [Required]
        [Display(Name = "模板用户Email")]
        public string TemplateUserEmail { get; set; }

        [BindProperty]
        [Required]
        [Display(Name = "克隆数量")]
        [Range(0, 30)]
        public int CloneCount { get; set; }

        [BindProperty]
        [Required]
        [Display(Name = "邮箱格式")]
        public string EmailFormat { get; set; }

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
            var guild = await CheckUserPrivilege();
            if (guild is null)
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
                return RedirectToPage("/Home/Index");
            }

            var currentCount = await _context.Users
                .Where(u => u.GuildID == guild.GuildID)
                .CountAsync();

            if (CloneCount <= 0)
            {
                StatusMessage = "错误：克隆数量无效。";
                return RedirectToPage();
            }
            if (CloneCount + currentCount > 30)
            {
                StatusMessage = "错误：公会人数不得超过30人。";
                return RedirectToPage();
            }

            var templateUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == TemplateUserEmail);
            if (templateUser is null)
            {
                StatusMessage = "错误：模板用户不存在。";
                return RedirectToPage();
            }

            var templateUserConfigs = await _context.UserCharacterConfigs
                .Where(cc => cc.UserID == templateUser.Id).ToListAsync();
            var templateUserVariants = await _context.UserZhouVariants
                .Where(v => v.UserID == templateUser.Id).ToListAsync();

            for (int i = 0; i < CloneCount; ++i)
            {
                var email = string.Format(EmailFormat, i + 1).ToUpperInvariant();
                if (await _context.Users.AnyAsync(u => u.NormalizedEmail == email))
                {
                    StatusMessage = $"错误：用户{string.Format(EmailFormat, i + 1)}已存在。";
                    return RedirectToPage();
                }
            }

            for (int i = 0; i < CloneCount; ++i)
            {
                var email = string.Format(EmailFormat, i + 1);
                var user = new PcrIdentityUser()
                {
                    Email = null,
                    UserName = email,
                    GameID = templateUser.GameID + "_" + (i + 1).ToString(),
                    EmailConfirmed = true,
                    GuildID = templateUser.GuildID,
                };
                var r = await _userManager.CreateAsync(user);
                if (r.Succeeded)
                {
                    //Clone configs.
                    foreach (var cc in templateUserConfigs)
                    {
                        _context.UserCharacterConfigs.Add(new()
                        {
                            UserID = user.Id,
                            CharacterConfigID = cc.CharacterConfigID,
                        });
                    }

                    //Clone variants.
                    foreach (var v in templateUserVariants)
                    {
                        _context.UserZhouVariants.Add(new()
                        {
                            UserID = user.Id,
                            ZhouVariantID = v.ZhouVariantID,
                            Borrow = v.Borrow,
                        });
                    }

                    //Create combos.
                    await FindAllCombos.Run(_context, user);

                    await _context.SaveChangesAsync();
                }
            }

            StatusMessage = "克隆完成。";
            return RedirectToPage("/Guild/Edit");
        }
    }
}
