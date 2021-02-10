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
        private readonly InMemoryStorageContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public CloneUsersModel(InMemoryStorageContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [TempData]
        public string StatusMessage { get; set; }
        public string StatusMessage2 { get; set; }

        [BindProperty]
        [Required]
        [Display(Name = "ģ���û�Email")]
        public string TemplateUserEmail { get; set; }

        [BindProperty]
        [Required]
        [Display(Name = "��¡����")]
        [Range(0, 30)]
        public int CloneCount { get; set; }

        [BindProperty]
        [Required]
        [Display(Name = "�����ʽ")]
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
            var guild = await _context.DbContext.Guilds
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

            var currentCount = await _context.DbContext.Users
                .Where(u => u.GuildID == guild.GuildID)
                .CountAsync();

            if (CloneCount <= 0)
            {
                StatusMessage2 = "���󣺿�¡������Ч��";
                return Page();
            }
            if (CloneCount + currentCount > 30)
            {
                StatusMessage2 = "���󣺹����������ó���30�ˡ�";
                return Page();
            }

            var templateUser = await _context.DbContext.Users
                .FirstOrDefaultAsync(u => u.Email == TemplateUserEmail);
            if (templateUser is null)
            {
                StatusMessage2 = "����ģ���û������ڡ�";
                return Page();
            }
            if (templateUser.GuildID != guild.GuildID)
            {
                StatusMessage2 = "����ģ���û����ڱ����ᡣ";
                return Page();
            }

            var templateUserConfigs = await _context.DbContext.UserCharacterConfigs
                .Where(cc => cc.UserID == templateUser.Id).ToListAsync();

            for (int i = 0; i < CloneCount; ++i)
            {
                var email = string.Format(EmailFormat, i + 1).ToUpperInvariant();
                if (await _context.DbContext.Users.AnyAsync(u => u.NormalizedEmail == email))
                {
                    StatusMessage2 = $"�����û�{string.Format(EmailFormat, i + 1)}�Ѵ��ڡ�";
                    return Page();
                }
            }

            var imGuild = await _context.GetGuildAsync(guild.GuildID);
            var newUserList = new List<PcrIdentityUser>();

            for (int i = 0; i < CloneCount; ++i)
            {
                var email = string.Format(EmailFormat, i + 1);
                var user = new PcrIdentityUser()
                {
                    Email = null,
                    UserName = email,
                    GameID = templateUser.GameID + "_" + (i + 1).ToString(),
                    EmailConfirmed = true,
                    GuildID = guild.GuildID,
                };

                _context.DbContext.Users.Add(user);
                {
                    //Clone configs.
                    foreach (var cc in templateUserConfigs)
                    {
                        _context.DbContext.UserCharacterConfigs.Add(new()
                        {
                            User = user,
                            CharacterConfigID = cc.CharacterConfigID,
                        });
                    }
                }
                newUserList.Add(user);
            }

            await _context.DbContext.SaveChangesAsync();

            foreach (var user in newUserList)
            {
                imGuild.AddUser(user.Id, 0, templateUser.Id);
            }

            StatusMessage = "��¡��ɡ�";
            return RedirectToPage("/Guilds/Edit");
        }
    }
}
