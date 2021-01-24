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
                StatusMessage = "����δ�ҵ��ù��ᡣ";
                return RedirectToPage();
            }
            foreach (var member in guild.Members)
            {
                if (member.Email is null)
                {
                    _context.Users.Remove(member);
                }
            }
            guild.Members.Clear();
            _context.Guilds.Remove(guild);
            await _context.SaveChangesAsync();

            StatusMessage = "������ɾ����";
            return RedirectToPage();
        }
    }
}
