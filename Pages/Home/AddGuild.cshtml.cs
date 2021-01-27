using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Home
{
    public class AddGuildModel : PageModel
    {
        public static bool IsAllowed { get; set; } = false;

        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public AddGuildModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [BindProperty]
        public Guild Guild { get; set; }

        public async Task<IActionResult> OnGet()
        {
            if (!IsAllowed || !_signInManager.IsSignedIn(User))
            {
                return RedirectToPage("/Home/Index");
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || user.GuildID.HasValue)
            {
                return RedirectToPage("/Home/Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (!IsAllowed || !_signInManager.IsSignedIn(User))
            {
                return RedirectToPage("/Home/Index");
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || user.GuildID.HasValue)
            {
                return RedirectToPage("/Home/Index");
            }

            var guild = new Guild()
            {
                Name = Guild.Name,
                Description = Guild.Description,
                Members = new List<PcrIdentityUser>()
                {
                    user,
                },
                Owner = user,
                OwnerID = user.Id,
            };
            _context.Guilds.Add(guild);
            user.Guild = guild;
            user.IsGuildAdmin = true;
            _context.Users.Update(user);

            //Create default config for each character for the new guild.
            await foreach (var c in _context.Characters)
            {
                var config = new CharacterConfig
                {
                    Guild = guild,
                    CharacterID = c.CharacterID,
                    Kind = CharacterConfigKind.Default,
                    Name = "抽到了",
                    Description = "什么配置都可以。",
                };
                _context.CharacterConfigs.Add(config);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("/Home/Index");
        }
    }
}
