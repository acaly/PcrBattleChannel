using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;
using IndexAttribute = CsvHelper.Configuration.Attributes.IndexAttribute;

namespace PcrBattleChannel.Pages.Admin.Bosses
{
    [Authorize(Roles = "Admin")]
    public class ImportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ImportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class InputBoss
        {
            [Index(0)]
            public int Stage { get; set; }
            [Index(1)]
            public string ShortName { get; set; }
            [Index(2)]
            public string Name { get; set; }
            [Index(3)]
            public int Life { get; set; }
            [Index(4)]
            public float Score { get; set; }
        }

        [BindProperty]
        public string Input { get; set; }

        [BindProperty]
        [Display(Name = "删除已有数据")]
        public bool Reset { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };

            var stages = await _context.BattleStages
                .OrderBy(s => s.StartLap)
                .Select(s => s.BattleStageID)
                .ToListAsync();

            using var reader = new StringReader(Input);
            using var csv = new CsvReader(reader, config);
            var list = new List<Boss>();
            foreach (var boss in csv.GetRecords<InputBoss>())
            {
                list.Add(new Boss
                {
                    BattleStageID = stages[boss.Stage - 1],
                    ShortName = boss.ShortName,
                    Name = boss.Name,
                    Life = boss.Life,
                    Score = boss.Score,
                });
            }

            if (Reset)
            {
                _context.Bosses.RemoveRange(_context.Bosses);
            }

            //Have to save for each boss, because we need to keep the order.
            foreach (var boss in list)
            {
                _context.Bosses.Add(boss);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Admin/Bosses/Index");
        }
    }
}
