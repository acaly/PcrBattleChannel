using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public class ConfigsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public ConfigsModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public (Character Character, CharacterConfig[] Configs)[] Characters { get; set; }

        [BindProperty]
        public NewConfigModel NewConfig { get; set; }

        public class NewConfigModel
        {
            public int CharacterID { get; set; }

            [Display(Name = "类别")]
            public CharacterConfigKind Kind { get; set; }

            [Display(Name = "名称")]
            public string Name { get; set; }

            [Display(Name = "描述")]
            public string Description { get; set; }
        }

        private async Task<int?> CheckUserPrivilege()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return null;
            }
            var user = await _userManager.GetUserAsync(User);
            if (!user.GuildID.HasValue || !user.IsGuildAdmin)
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

            var characters = await _context.Characters
                .ToListAsync();
            var configs = await _context.CharacterConfigs
                .Include(cc => cc.Character)
                .Where(cc => cc.GuildID == guildID)
                .ToListAsync();

            Characters = configs
                .GroupBy(cc => cc.Character)
                .Select(g => (g.Key, g.ToArray()))
                .OrderBy(g => g.Item1.InternalID)
                .ToArray();

            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            var guildID = await CheckUserPrivilege();
            if (!guildID.HasValue)
            {
                return Forbid();
            }

            if (NewConfig.Kind <= CharacterConfigKind.Default ||
                NewConfig.Kind > CharacterConfigKind.Others)
            {
                return StatusCode(400);
            }

            if (!await _context.Characters.AnyAsync(c => c.CharacterID == NewConfig.CharacterID))
            {
                return StatusCode(400);
            }

            var config = new CharacterConfig()
            {
                GuildID = guildID.Value,
                CharacterID = NewConfig.CharacterID,
                Kind = NewConfig.Kind,
                Name = NewConfig.Name,
                Description = NewConfig.Description,
            };
            _context.Add(config);
            await _context.SaveChangesAsync();

            return Partial("_Configs_ConfigPartial", config);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int? id)
        {
            var guildID = await CheckUserPrivilege();
            if (!guildID.HasValue)
            {
                return Forbid();
            }

            if (!id.HasValue)
            {
                return NotFound();
            }

            var config = await _context.CharacterConfigs.FindAsync(id);
            if (config is null)
            {
                return NotFound();
            }

            _context.CharacterConfigs.Remove(config);
            await _context.SaveChangesAsync();

            return StatusCode(200);
        }
    }
}
