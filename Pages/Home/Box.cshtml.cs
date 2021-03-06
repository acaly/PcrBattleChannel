﻿using System;
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

        public bool IsAdmin { get; private set; }
        public List<(Character character, CharacterConfig[] configs)>[] CharacterConfigs { get; private set; }
        public Dictionary<int, int> IncludedConfigs { get; private set; } //CC id -> UserCC id.

        public async Task<IActionResult> OnGetAsync()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToPage("/Home/Index");
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue)
            {
                return RedirectToPage("/Home/Index");
            }
            IsAdmin = user.IsGuildAdmin;

            //All configs.

            var allConfigs = await _context.CharacterConfigs
                .Include(c => c.Character)
                .Include(c => c.Guild)
                .Where(c => c.GuildID == user.GuildID)
                .ToListAsync();

            var grouped = allConfigs
                .Where(c => c.Kind != CharacterConfigKind.Rank || c.Name.Length == 0 || c.Name[^1] != 'X') //Exclude Ra-X.
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
            CharacterConfigs = new[]
            {
                groupedConv.Where(c => c.character.Rarity == 1).ToList(),
                groupedConv.Where(c => c.character.Rarity == 2).ToList(),
                groupedConv.Where(c => c.character.Rarity == 3).ToList(),
            };

            //Existing configs (for the user).

            try
            {
                var existingConfigs = await _context.UserCharacterConfigs
                    .Where(cc => cc.UserID == user.Id)
                    .ToListAsync();

                IncludedConfigs = existingConfigs.ToDictionary(cc => cc.CharacterConfigID, cc => cc.UserCharacterConfigID);
            }
            catch
            {
                await _context.UserCharacterConfigs.Where(cc => cc.UserID == user.Id).DeleteFromQueryAsync();
                IncludedConfigs = new();
            }

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
            if (user is null || !user.GuildID.HasValue)
            {
                return StatusCode(400);
            }

            var ccidv = ccid.Value;
            var cc = await _context.CharacterConfigs
                .FirstOrDefaultAsync(ccx => ccx.CharacterConfigID == ccidv);
            if (cc is null || cc.GuildID != user.GuildID.Value)
            {
                return StatusCode(400);
            }

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

            if (cc.Kind == CharacterConfigKind.Rank && cc.Name.Length > 0 && cc.Name[^1] != 'X')
            {
                var namePrefix = cc.Name[..^1];
                var autoName = cc.Name[..^1] + "X";
                var autoConfig = await _context.CharacterConfigs
                    .FirstOrDefaultAsync(ccx =>
                        ccx.GuildID == user.GuildID.Value &&
                        ccx.CharacterID == cc.CharacterID &&
                        ccx.Kind == CharacterConfigKind.Rank &&
                        ccx.Name == autoName);
                if (autoConfig is not null)
                {
                    var userAutoConfig = await _context.UserCharacterConfigs
                        .FirstOrDefaultAsync(uccx => uccx.CharacterConfigID == autoConfig.CharacterConfigID && uccx.UserID == user.Id);
                    if (ucc is null && userAutoConfig is null)
                    {
                        //Add (easy).
                        _context.UserCharacterConfigs.Add(new UserCharacterConfig
                        {
                            UserID = user.Id,
                            CharacterConfigID = autoConfig.CharacterConfigID,
                        });
                    }
                    else if (ucc is not null && userAutoConfig is not null)
                    {
                        //Remove (hard).
                        var matchingUserConfigs = await _context.UserCharacterConfigs
                            .Include(uccx => uccx.CharacterConfig)
                            .Where(uccx => uccx.UserID == user.Id &&
                                uccx.CharacterConfig.CharacterID == cc.CharacterID &&
                                uccx.CharacterConfig.Kind == CharacterConfigKind.Rank &&
                                uccx.CharacterConfig.Name.StartsWith(namePrefix))
                            .CountAsync();
                        if (matchingUserConfigs == 2)
                        {
                            _context.UserCharacterConfigs.Remove(userAutoConfig);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            return StatusCode(200);
        }
    }
}
