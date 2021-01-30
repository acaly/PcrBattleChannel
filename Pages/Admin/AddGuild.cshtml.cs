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
            [Display(Name = "��������")]
            public string GuildName { get; set; }
            [Display(Name = "�᳤Email")]
            public string OwnerEmail { get; set; }
        }

        [TempData]
        public string StatusMessage { get; set; }
        public string StatusMessage2 { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Input = new InputModel
            {
                GuildName = "",
                OwnerEmail = "",
            };
            if (!await _context.Bosses.AnyAsync())
            {
                StatusMessage2 = "���棺��������ǰ���������������ݺ�Boss���ݣ�";
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var owner = _context.Users
                .FirstOrDefault(xx => xx.Email == Input.OwnerEmail);

            if (owner is null)
            {
                StatusMessage2 = "���󣺻᳤�û������ڡ�";
                return Page();
            }

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
                    Name = "�鵽��",
                    Description = "ʲô���ö����ԡ�",
                };
                _context.CharacterConfigs.Add(config);
            }

            //Create default boss plans.
            foreach (var b in _context.Bosses)
            {
                var s = new GuildBossStatus
                {
                    Guild = guild,
                    BossID = b.BossID,
                    IsPlan = true,
                    DamageRatio = 1,
                };
                _context.GuildBossStatuses.Add(s);
            }

            await _context.SaveChangesAsync();

            StatusMessage = "�����Ѵ�����";
            return RedirectToPage();
        }
    }
}
