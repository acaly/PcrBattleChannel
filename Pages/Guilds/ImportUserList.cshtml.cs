using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Guilds
{
    public class ImportUserListModel : PageModel
    {
        private readonly InMemoryStorageContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public ImportUserListModel(InMemoryStorageContext context, SignInManager<PcrIdentityUser> signInManager,
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
        public string Input { get; set; }

        [BindProperty]
        [DataType(DataType.Password)]
        [Display(Name = "设置密码")]
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
            var guild = await _context.DbContext.Guilds
                .FirstOrDefaultAsync(m => m.GuildID == user.GuildID.Value);
            if (guild.OwnerID != user.Id)
            {
                return null;
            }
            return guild;
        }

        public async Task<IActionResult> OnGet()
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

            var userList = new List<PcrIdentityUser>();
            using (var sr = new StringReader(Input))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var qq = ulong.Parse(line);
                    var name = qq.ToString();
                    var email = $"{qq}@qq.com";
                    if (await _context.DbContext.Users.AnyAsync(u => u.QQID == qq || u.Email == email || u.UserName == name))
                    {
                        StatusMessage2 = $"错误：QQ {qq} 已经注册。";
                        return Page();
                    }
                    var user = new PcrIdentityUser
                    {
                        QQID = qq,
                        UserName = name,
                        Email = email,
                        GuildID = guild.GuildID,
                    };
                    userList.Add(user);
                    //Not creating with UserManager. This allows to create without saving.
                    _context.DbContext.Users.Add(user);
                }
            }
            await _context.DbContext.SaveChangesAsync();
            if (!string.IsNullOrEmpty(Password))
            {
                foreach (var u in userList)
                {
                    await _userManager.AddPasswordAsync(u, Password);
                }
            }

            var imGuild = await _context.GetGuildAsync(guild.GuildID);
            foreach (var u in userList)
            {
                imGuild.AddUser(u.Id, 0, null);
            }

            StatusMessage = "添加完成。";
            return RedirectToPage("/Guilds/Edit");
        }
    }
}
