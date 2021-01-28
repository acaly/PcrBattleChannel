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
using PcrBattleChannel.Algorithm;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Zhous
{
    public class ImportModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;
        private readonly ZhouParserFactory _parserFactory;

        public ImportModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager, ZhouParserFactory parserFactory)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
            _parserFactory = parserFactory;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public string Input { get; set; }

        [BindProperty]
        [Display(Name = "合并角色相同的轴作为详细配置")]
        public bool Merge { get; set; } = true;

        [BindProperty]
        [Display(Name = "在导入数据中指定轴名")]
        public bool HasName { get; set; } = false;

        private async Task<int?> CheckUserPrivilege()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return null;
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue || !user.IsGuildAdmin)
            {
                return null;
            }
            return user.GuildID;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var guildID = await CheckUserPrivilege();
            if (!guildID.HasValue)
            {
                return RedirectToPage("/Guild/Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var guildID = await CheckUserPrivilege();
            if (!guildID.HasValue)
            {
                return RedirectToPage("/Guild/Index");
            }

            var parser = _parserFactory.GetParser(_context, guildID.Value);
            await parser.ReadDatabase();

            var list = new List<Zhou>();
            int lineNum = -1;
            try
            {
                using var reader = new StringReader(Input);
                string line;
                while ((line = reader.ReadLine()) is not null)
                {
                    lineNum += 1;
                    var z = parser.Parse(line, HasName);
                    if (z is not null)
                    {
                        list.Add(z);
                    }
                }
            }
            catch
            {
                StatusMessage = $"错误：读取轴表第{lineNum + 1}行失败。";
                return Page();
            }

            var unsavedMergeCheck = new List<Zhou>();

            if (Merge)
            {
                foreach (var z in list)
                {
                    var existing = await _context.Zhous
                        .FirstOrDefaultAsync(zz =>
                            zz.GuildID == guildID.Value &&
                            zz.C1ID == z.C1ID &&
                            zz.C2ID == z.C2ID &&
                            zz.C3ID == z.C3ID &&
                            zz.C4ID == z.C4ID &&
                            zz.C5ID == z.C5ID); //Assuming same order (by range).
                    if (existing is null)
                    {
                        existing = unsavedMergeCheck.FirstOrDefault(zz =>
                            zz.C1ID == z.C1ID &&
                            zz.C2ID == z.C2ID &&
                            zz.C3ID == z.C3ID &&
                            zz.C4ID == z.C4ID &&
                            zz.C5ID == z.C5ID);
                    }
                    var v = ((List<ZhouVariant>)z.Variants)[0];
                    if (existing is not null)
                    {
                        v.ZhouID = existing.ZhouID;
                        v.Zhou = existing;
                        _context.ZhouVariants.Add(v);
                        await EditModel.CheckAndAddUserVariants(_context, guildID.Value, existing, v, v.CharacterConfigs);
                    }
                    else
                    {
                        _context.Zhous.Add(z);
                        await EditModel.CheckAndAddUserVariants(_context, guildID.Value, z, v, v.CharacterConfigs);
                        unsavedMergeCheck.Add(z);
                    }
                }
            }
            else
            {
                _context.Zhous.AddRange(list);
                foreach (var z in list)
                {
                    var v = ((List<ZhouVariant>)z.Variants)[0];
                    await EditModel.CheckAndAddUserVariants(_context, guildID.Value, z, v, v.CharacterConfigs);
                }
            }

            await _context.SaveChangesAsync();

            StatusMessage = "导入成功。";
            return RedirectToPage();
        }
    }
}
