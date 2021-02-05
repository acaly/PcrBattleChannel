using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class RemoveGuildModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RemoveGuildModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public List<Guild> Guilds { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var g = await _context.Guilds.Include(g => g.Owner).ToListAsync();
            Guilds = g;
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var guild = await _context.Guilds
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GuildID == id);
            if (guild is null)
            {
                StatusMessage = "错误：未找到该公会。";
                return RedirectToPage();
            }
            foreach (var member in guild.Members)
            {
                //Character status does not depend on guild. We have to delete those here, or it
                //will be carried to the next guild.
                _context.UserCharacterStatuses.RemoveRange(_context.UserCharacterStatuses.Where(c => c.UserID == member.Id));

                if (member.Email is null)
                {
                    //TODO
                    //Something here is preventing user from being deleted.
                    //(May be the UserCharacterStatuses above, or the combo info that has been moved to InMemoryStorage.)
                    //Need to figure this out and use cascade deletion.
                    //Also remember to fix the remove user page in Guilds/Edit.
                    _context.UserCharacterConfigs.RemoveRange(_context.UserCharacterConfigs.Where(c => c.UserID == member.Id));
                    _context.Users.Remove(member);
                }
                else
                {
                    member.Attempts = 0;
                    member.GuessedAttempts = 0;
                    member.IsIgnored = false;
                }
                member.Attempt1ID = member.Attempt2ID = member.Attempt3ID = null;
                member.Attempt1Borrow = member.Attempt2Borrow = member.Attempt3Borrow = null;
            }
            guild.Members.Clear();
            _context.Guilds.Remove(guild);
            await _context.SaveChangesAsync();

            StatusMessage = "公会已删除。";
            return RedirectToPage();
        }
    }
}
