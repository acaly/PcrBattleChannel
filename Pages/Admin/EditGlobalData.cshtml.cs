﻿using System;
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

        [Display(Name = "各阶段起始周目")]
        [BindProperty]
        public string StageText { get; set; }

        [Display(Name = "重置赛季数据")]
        [BindProperty]
        public bool ResetData { get; set; }

        [Display(Name = "导入角色数据")]
        [BindProperty]
        public bool ResetCharacters { get; set; }

        private async Task<string> GetStageText(int globalDataID)
        {
            var stages = await _context.BattleStages
                .Where(s => s.GlobalDataID == globalDataID)
                .Select(s => s.Order)
                .ToListAsync();
            return string.Join(',', stages);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            GlobalData = await _context.GlobalData.FirstOrDefaultAsync();

            if (GlobalData == null)
            {
                GlobalData = new();
            }
            else
            {
                StageText = await GetStageText(GlobalData.GlobalDataID);
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

        private static readonly string[] _longNameNumbers = new[]
        {
            "一", "二", "三", "四", "五", "六", "七", "八", "九", "十",
        };

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

            var stageText = string.IsNullOrWhiteSpace(StageText) ? "1" : StageText;
            var stages = stageText.Split(',');
            for (int i = 0; i < stages.Length; ++i)
            {
                var stage = new BattleStage
                {
                    GlobalData = newData,
                    Order = i,
                    Name = $"第{_longNameNumbers[i]}阶段",
                    ShortName = new string((char)('A' + i), 1),
                    StartLap = int.Parse(stages[i]) - 1,
                };
                _context.BattleStages.Add(stage);
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
                    Range = float.Parse(fields[4]),
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
