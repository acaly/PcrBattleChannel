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

namespace PcrBattleChannel.Pages.Zhous
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;

        public IndexModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public bool IsAdmin { get; set; }
        public bool IsOwner { get; set; }
        public Guild Guild { get; set; }
        public List<Zhou> Zhou { get; set; }
        public HashSet<int> UserZhouSettings { get; set; }

        private async Task<PcrIdentityUser> CheckUserPrivilege()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return null;
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue)
            {
                return null;
            }
            Guild = await _context.Guilds.FirstOrDefaultAsync(g => g.GuildID == user.GuildID);
            if (Guild is null)
            {
                return null;
            }
            IsOwner = Guild.OwnerID == user.Id;
            return user;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await CheckUserPrivilege();
            if (user is null)
            {
                return RedirectToPage("/Index");
            }
            IsAdmin = user.IsGuildAdmin;

            Zhou = await _context.Zhous
                .Include(z => z.Boss)
                .Include(z => z.C1)
                .Include(z => z.C2)
                .Include(z => z.C3)
                .Include(z => z.C4)
                .Include(z => z.C5)
                .Include(z => z.Guild)
                .Where(z => z.GuildID == user.GuildID).ToListAsync();

            var allSettings = await _context.UserZhouVariants
                .Include(uv => uv.ZhouVariant)
                .Where(uv => uv.UserID == user.Id)
                .ToListAsync();
            UserZhouSettings = allSettings
                .GroupBy(uv => uv.ZhouVariant.ZhouID)
                .Select(g => g.Key).ToHashSet();

            return Page();
        }

        public async Task<IActionResult> OnPostRefreshAsync()
        {
            var user = await CheckUserPrivilege();
            if (user is null)
            {
                return RedirectToPage("/Index");
            }

            await _context.UserZhouVariants
                .Where(v => v.UserID == user.Id)
                .DeleteFromQueryAsync();
            var allZhous = _context.ZhouVariants
                .Include(z => z.Zhou)
                .Include(z => z.CharacterConfigs)
                .Where(z => z.Zhou.GuildID == user.GuildID);
            var userAllConfigs = await _context.UserCharacterConfigs
                .Include(c => c.CharacterConfig)
                .Where(c => c.UserID == user.Id)
                .ToListAsync();
            var userCharacters = userAllConfigs
                .Where(c => c.CharacterConfig.Kind == default)
                .Select(c => c.CharacterConfig.CharacterID)
                .ToHashSet();
            var userAllConfigIds = userAllConfigs
                .Select(c => c.CharacterConfigID)
                .ToHashSet();

            await allZhous.ForEachAsync(z =>
            {
                int? borrowId = null;
                void SetBorrow(int index)
                {
                    borrowId = borrowId.HasValue ? -1 : index;
                }
                if (!userCharacters.Contains(z.Zhou.C1ID.Value)) SetBorrow(0);
                if (!userCharacters.Contains(z.Zhou.C2ID.Value)) SetBorrow(1);
                if (!userCharacters.Contains(z.Zhou.C3ID.Value)) SetBorrow(2);
                if (!userCharacters.Contains(z.Zhou.C4ID.Value)) SetBorrow(3);
                if (!userCharacters.Contains(z.Zhou.C5ID.Value)) SetBorrow(4);
                foreach (var c in z.CharacterConfigs)
                {
                    if (c.CharacterConfigID.HasValue && !userAllConfigIds.Contains(c.CharacterConfigID.Value))
                    {
                        SetBorrow(c.CharacterIndex);
                    }
                }
                if (borrowId != -1)
                {
                    _context.UserZhouVariants.Add(new UserZhouVariant
                    {
                        Borrow = borrowId,
                        UserID = user.Id,
                        ZhouVariantID = z.ZhouVariantID,
                    });
                }
            });
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAllAsync()
        {
            var user = await CheckUserPrivilege();
            if (user is null || !IsOwner)
            {
                return RedirectToPage("/Index");
            }
            var zhous = await _context.Zhous
                .Include(z => z.Variants)
                .Where(z => z.GuildID == Guild.GuildID)
                .ToListAsync();

            foreach (var zhou in zhous)
            {
                foreach (var v in zhou.Variants)
                {
                    await EditModel.CheckAndRemoveUserVariants(_context, v.ZhouVariantID);
                }
                zhou.Variants.Clear();
                _context.Zhous.Remove(zhou);
            }
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}
