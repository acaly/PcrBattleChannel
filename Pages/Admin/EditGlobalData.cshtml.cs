using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Admin
{
    public class EditGlobalDataModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditGlobalDataModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public GlobalData GlobalData { get; set; }

        [Display(Name = "重置赛季数据")]
        [BindProperty]
        public bool ResetData { get; set; }

        [Display(Name = "导入角色数据")]
        [BindProperty]
        public bool ResetCharacters { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            GlobalData = await _context.GlobalData.FirstOrDefaultAsync();

            if (GlobalData == null)
            {
                GlobalData = new();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (ResetData)
            {
                await DoResetDataAsync();
                return RedirectToPage("/Admin/Index");
            }

            _context.Attach(GlobalData).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GlobalDataExists(GlobalData.GlobalDataID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("/Admin/Index");
        }

        private async Task DoResetDataAsync()
        {
            //Remove all existing entries.
            if (_context.GlobalData.Any())
            {
                _context.GlobalData.RemoveRange(_context.GlobalData);
            }

            var newData = new GlobalData
            {
                SeasonName = GlobalData.SeasonName,
                StartTime = GlobalData.StartTime,
                EndTime = GlobalData.EndTime,
            };
            _context.GlobalData.Add(newData);

            if (ResetCharacters)
            {
                _context.Characters.RemoveRange(_context.Characters);
                ImportCharacters();
            }

            await _context.SaveChangesAsync();
        }

        private void ImportCharacters()
        {
            var res = @"PcrBattleChannel.Models.InitCharacterData.csv";
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(res);
            using var reader = new StreamReader(stream);

            //Skip title.
            var line = reader.ReadLine();

            //Read each row.
            while ((line = reader.ReadLine()) is not null)
            {
                var fields = line.Split(',');
                var c = new Character
                {
                    InternalID = int.Parse(fields[0]),
                    Name = fields[1],
                    Rarity = int.Parse(fields[2]),
                    HasWeapon = int.Parse(fields[3]) != 0,
                };
                _context.Characters.Add(c);
            }
        }

        private bool GlobalDataExists(int id)
        {
            return _context.GlobalData.Any(e => e.GlobalDataID == id);
        }
    }
}
