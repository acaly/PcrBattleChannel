using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Zhous
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public CreateModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public class InputModel
        {
            [Display(Name = "轴名")]
            public string Name { get; set; }

            [Display(Name = "说明")]
            public string Description { get; set; }

            [Display(Name = "Boss")]
            public int BossID { get; set; }

            [Display(Name = "阵容")]
            public string CharacterListString { get; set; }
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

        [BindProperty]
        public InputModel Input { get; set; }

        public List<Character> Characters { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var guildID = await CheckUserPrivilege();
            if (!guildID.HasValue)
            {
                return RedirectToPage("/Guild/Index");
            }
            ViewData["BossID"] = new SelectList(_context.Bosses, "BossID", "ShortName");
            Characters = await _context.Characters.OrderBy(c => c.Range).ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var guildID = await CheckUserPrivilege();
            if (!guildID.HasValue)
            {
                return RedirectToPage("/Guild/Index");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var zhou = new Zhou
            {
                GuildID = guildID.Value,
                Name = Input.Name,
                Description = Input.Description,
                BossID = Input.BossID,
            };

            var characters = Input.CharacterListString?.Split(',') ?? Array.Empty<string>()
                .Distinct().ToArray();
            if (characters.Length != 5)
            {
                //Many of the algorithms assume every Zhou has 5 characters.
                return RedirectToPage("./Index");
            }

            (float range, int id)[] characterData = new (float, int)[5];
            for (int i = 0; i < 5; ++i)
            {
                if (!int.TryParse(characters[i], out var id))
                {
                    return RedirectToPage("./Index");
                }
                var ch = await _context.Characters.FindAsync(id);
                if (ch is null)
                {
                    return RedirectToPage("./Index");
                }
                characterData[i] = (ch.Range, ch.CharacterID);
            }
            Array.Sort(characterData, (a, b) => Math.Sign(a.range - b.range));

            zhou.C1ID = characterData[0].id;
            zhou.C2ID = characterData[1].id;
            zhou.C3ID = characterData[2].id;
            zhou.C4ID = characterData[3].id;
            zhou.C5ID = characterData[4].id;

            _context.Zhous.Add(zhou);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
