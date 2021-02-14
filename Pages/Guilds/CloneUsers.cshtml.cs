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
        [Display(Name = "ģ���û�QQ")]
        public ulong TemplateUserQQ { get; set; }

        [BindProperty]
        [Required]
        [Display(Name = "��¡����")]
        [Range(0, 30)]
        public int CloneCount { get; set; }

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
                .FirstOrDefaultAsync(u => u.QQID == TemplateUserQQ);
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

            const string EmailFormat = "clone_{0}_{1}@example.com";
            var cloneTime = DateTime.UtcNow.Ticks.ToString();

            var templateUserConfigs = await _context.DbContext.UserCharacterConfigs
                .Where(cc => cc.UserID == templateUser.Id).ToListAsync();

            var newUserList = new List<PcrIdentityUser>();
            for (int i = 0; i < CloneCount; ++i)
            {
                var email = string.Format(EmailFormat, cloneTime, i).ToUpperInvariant();
                var user = new PcrIdentityUser()
                {
                    Email = null,
                    UserName = email,
                    GameID = templateUser.GameID + "_" + (i + 1).ToString(),
                    EmailConfirmed = true,
                    GuildID = guild.GuildID,
                };
                newUserList.Add(user);
                if (await _context.DbContext.Users.AnyAsync(u => u.NormalizedEmail == email))
                {
                    StatusMessage2 = $"�����û�{email}�Ѵ��ڡ������³��ԡ�";
                    return Page();
                }
            }

            var imGuild = await _context.GetGuildAsync(guild.GuildID);

            for (int i = 0; i < CloneCount; ++i)
            {
                _context.DbContext.Users.Add(newUserList[i]);
                {
                    //Clone configs.
                    foreach (var cc in templateUserConfigs)
                    {
                        _context.DbContext.UserCharacterConfigs.Add(new()
                        {
                            User = newUserList[i],
                            CharacterConfigID = cc.CharacterConfigID,
                        });
                    }
                }
                newUserList.Add(newUserList[i]);
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
