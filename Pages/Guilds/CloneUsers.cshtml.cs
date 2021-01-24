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

        [BindProperty]
        [Required]
        [Display(Name = "模板用户Email")]
        public string TemplateUserEmail { get; set; }

        [BindProperty]
        [Required]
        [Display(Name = "克隆数量")]
        public int CloneCount { get; set; }

        [BindProperty]
        [Required]
        [Display(Name = "邮箱格式")]
        public string EmailFormat { get; set; }

        [BindProperty]
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; }

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
                return RedirectToPage("/Guild/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var guild = await CheckUserPrivilege();
            if (guild is null)
            {
                return RedirectToPage("/Guild/Index");
            }

            var currentCount = await _context.Users
                .Where(u => u.GuildID == guild.GuildID)
                .CountAsync();

            if (CloneCount == 0 || CloneCount + currentCount > 30)
            {
                return RedirectToPage("/Guild/Index");
            }

            var templateUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == TemplateUserEmail);

            var templateUserConfigs = await _context.UserCharacterConfigs
                .Where(cc => cc.UserID == templateUser.Id).ToListAsync();
            var templateUserVariants = await _context.UserZhouVariants
                .Where(v => v.UserID == templateUser.Id).ToListAsync();

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
                var r = await _userManager.CreateAsync(user, Password);
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

            return RedirectToPage("/Guild/Index");
        }
    }
}
