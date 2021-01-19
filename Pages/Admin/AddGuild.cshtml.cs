using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    using Guild = PcrBattleChannel.Models.Guild;

    [Authorize(Roles = "Admin")]
    public class AddGuildModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public AddGuildModel(ApplicationDbContext context, UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class InputModel
        {
            [Display(Name = "公会名称")]
            public string GuildName { get; set; }
            [Display(Name = "会长")]
            public string OwnerEmail { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public IActionResult OnGetAsync()
        {
            Input = new InputModel
            {
                GuildName = "Name",
                OwnerEmail = "",
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var owner = _context.Users.FirstOrDefault(xx => xx.Email == Input.OwnerEmail);

            var guild = new Guild()
            {
                Name = Input.GuildName,
                Members = new List<PcrIdentityUser>()
                {
                    owner,
                },
                Owner = owner,
                OwnerID = owner.Id,
            };
            _context.Guilds.Add(guild);
            owner.Guild = guild;
            owner.IsGuildAdmin = true;
            _context.Users.Update(owner);

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

            StatusMessage = "公会已创建。";
            return RedirectToPage();
        }
    }
}
