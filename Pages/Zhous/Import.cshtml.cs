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
        [Display(Name = "�ϲ���ͬ��ɫ������Ϊ��ϸ����")]
        public bool Merge { get; set; } = true;

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
                    var z = parser.Parse(line);
                    if (z is not null)
                    {
                        list.Add(z);
                    }
                }
            }
            catch
            {
                StatusMessage = $"���󣺶�ȡ����{lineNum + 1}��ʧ�ܡ�";
                return Page();
            }

            //TODO merge
            if (Merge)
            {
                foreach (var z in list)
                {
                    var existing = await _context.Zhous
                        .FirstOrDefaultAsync(zz =>
                            z.GuildID == guildID &&
                            z.C1ID == zz.C1ID &&
                            z.C2ID == zz.C2ID &&
                            z.C3ID == zz.C3ID &&
                            z.C4ID == zz.C4ID &&
                            z.C5ID == zz.C5ID); //Assuming same order (by range).
                    var v = ((List<ZhouVariant>)z.Variants)[0];
                    if (existing is not null)
                    {
                        v.ZhouID = existing.ZhouID;
                        _context.ZhouVariants.Add(v);
                    }
                    else
                    {
                        _context.Zhous.Add(z);
                    }
                    await EditModel.CheckAndAddUserVariants(_context, guildID.Value, z, v, v.CharacterConfigs);
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

            StatusMessage = "����ɹ���";
            return RedirectToPage();
        }
    }
}
