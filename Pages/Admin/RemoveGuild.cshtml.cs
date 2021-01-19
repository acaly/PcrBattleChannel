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
    using Guild = PcrBattleChannel.Models.Guild;

    [Authorize(Roles = "Admin")]
    public class RemoveGuildModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RemoveGuildModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Guild> Guilds { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var g = await _context.Guilds.Include(g => g.Owner).ToListAsync();
            Guilds = g;
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var guild = await _context.Guilds.Include(g => g.Members).FirstOrDefaultAsync(g => g.GuildID == id);
            if (guild is null)
            {
                return RedirectToPage();
            }
            guild.Members.Clear();
            _context.Guilds.Remove(guild);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}
