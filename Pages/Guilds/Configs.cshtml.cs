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

        [TempData]
        public string StatusMessage { get; set; }

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
            if (user is null || !user.GuildID.HasValue || !user.IsGuildAdmin)
            {
                return null;
            }
            return user.GuildID;
        }

        public static async Task CheckAndAddRankConfigAsync(ApplicationDbContext context, CharacterConfig cc,
            List<UserCharacterConfig> output)
        {
            if (cc.Kind != CharacterConfigKind.Rank || cc.Name.Length == 0 && cc.Name[^1] != 'X')
            {
                return;
            }
            var searchName = cc.Name[..^1];
            var affectedConfigs = await context.CharacterConfigs
                .Where(xcc => xcc.GuildID == cc.GuildID && xcc.CharacterID == cc.CharacterID && xcc.Name.StartsWith(searchName))
                .Select(xcc => xcc.CharacterConfigID)
                .ToListAsync();
            var affectedUsers = new HashSet<string>();
            foreach (var actualConfig in affectedConfigs)
            {
                affectedUsers.UnionWith(await context.UserCharacterConfigs
                    .Where(ucc => ucc.CharacterConfigID == actualConfig)
                    .Select(ucc => ucc.UserID)
                    .ToListAsync());
            }
            foreach (var uid in affectedUsers)
            {
                var ucc = new UserCharacterConfig
                {
                    UserID = uid,
                    CharacterConfig = cc,
                };
                context.UserCharacterConfigs.Add(ucc);
                output?.Add(ucc);
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var guildID = await CheckUserPrivilege();
            if (!guildID.HasValue)
            {
                return RedirectToPage("/Guilds/Index");
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
                return StatusCode(400);
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

            var config = new CharacterConfig
            {
                GuildID = guildID.Value,
                CharacterID = NewConfig.CharacterID,
                Kind = NewConfig.Kind,
                Name = NewConfig.Name,
                Description = NewConfig.Description,
            };
            _context.Add(config);
            await CheckAndAddRankConfigAsync(_context, config, null);
            await _context.SaveChangesAsync();

            return Partial("_Configs_ConfigPartial", config);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int? id)
        {
            var guildID = await CheckUserPrivilege();
            if (!guildID.HasValue)
            {
                return StatusCode(400);
            }

            if (!id.HasValue)
            {
                return StatusCode(400);
            }

            var config = await _context.CharacterConfigs.FindAsync(id);
            if (config is null)
            {
                return StatusCode(400);
            }

            _context.CharacterConfigs.Remove(config);
            await _context.SaveChangesAsync();

            return StatusCode(200);
        }
    }
}
