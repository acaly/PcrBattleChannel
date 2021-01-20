using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Home
{
    public class BoxModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public BoxModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public List<(Character character, CharacterConfig[] configs)> CharacterConfigs { get; set; }
        public Dictionary<int, int> IncludedConfigs { get; set; } //CC id -> UserCC id.

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return Redirect("/");
            }
            var user = await _userManager.GetUserAsync(User);
            if (!user.GuildID.HasValue)
            {
                return Redirect("/");
            }

            //All configs.

            var allConfigs = await _context.CharacterConfigs
                .Include(c => c.Character)
                .Include(c => c.Guild)
                .Where(c => c.GuildID == user.GuildID)
                .ToListAsync();

            var grouped = allConfigs
                .GroupBy(c => c.CharacterID)
                .Select(g => (g.Key, g.OrderBy(c => c.Kind).ToArray()));

            List<(Character character, CharacterConfig[] configs)> groupedConv = new();
            foreach (var (key, value) in grouped)
            {
                var c = await _context.Characters
                    .FirstOrDefaultAsync(c => c.CharacterID == key);
                groupedConv.Add((c, value));
            }
            groupedConv.Sort((g1, g2) => Math.Sign(g1.character.Range - g2.character.Range));
            CharacterConfigs = groupedConv;

            //Existing configs (for the user).

            var existingConfigs = await _context.UserCharacterConfigs
                .Where(cc => cc.UserID == user.Id)
                .ToListAsync();

            IncludedConfigs = existingConfigs.ToDictionary(cc => cc.CharacterConfigID, cc => cc.UserCharacterConfigID);

            return Page();
        }

        //Ajax
        public async Task<IActionResult> OnPostUpdateAsync(int? ccid)
        {
            if (!ccid.HasValue)
            {
                return StatusCode(400);
            }
            if (!_signInManager.IsSignedIn(User))
            {
                return StatusCode(400);
            }
            var user = await _userManager.GetUserAsync(User);
            if (!user.GuildID.HasValue)
            {
                return StatusCode(400);
            }

            var ccidv = ccid.Value;
            var ucc = await _context.UserCharacterConfigs
                .FirstOrDefaultAsync(x => x.CharacterConfigID == ccidv && x.UserID == user.Id);
            if (ucc is null)
            {
                _context.UserCharacterConfigs.Add(new UserCharacterConfig
                {
                    UserID = user.Id,
                    CharacterConfigID = ccid.Value,
                });
            }
            else
            {
                _context.UserCharacterConfigs.Remove(ucc);
            }
            await _context.SaveChangesAsync();

            return StatusCode(200);
        }
    }
}
