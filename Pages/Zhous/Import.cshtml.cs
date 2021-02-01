using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PcrBattleChannel.Algorithm;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;

namespace PcrBattleChannel.Pages.Zhous
{
    public class ImportModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<PcrIdentityUser> _signInManager;
        private readonly UserManager<PcrIdentityUser> _userManager;
        private readonly ZhouParserFactory _parserFactory;

        public ImportModel(ApplicationDbContext context, SignInManager<PcrIdentityUser> signInManager,
            UserManager<PcrIdentityUser> userManager, ZhouParserFactory parserFactory)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
            _parserFactory = parserFactory;
        }

        public enum ZhouParserDuplicateCheckBehavior
        {
            [Display(Name = "忽略")]
            Ignore, //Add the new zhou, ignoring the old one. (Might be merged into the same Zhou.)
            [Display(Name = "提示")]
            Error, //Show error message and do nothing.
            [Display(Name = "覆盖")]
            Modify, //Update the old zhou.
            [Display(Name = "跳过")]
            Return, //Skip the new zhou and return those skipped to user by keeping them in the input box.
        }

        [TempData]
        public string StatusMessage { get; set; }
        public string StatusMessage2 { get; set; }

        [BindProperty]
        public string Input { get; set; }

        [BindProperty]
        [Display(Name = "合并角色相同的轴作为详细配置")]
        public bool Merge { get; set; } = true;

        [BindProperty]
        [Display(Name = "在导入数据中指定轴名")]
        public bool HasName { get; set; } = false;

        [BindProperty]
        [Display(Name = "导入为草稿")]
        public bool AsDraft { get; set; } = false;

        [BindProperty]
        [Display(Name = "自动创建缺少的角色配置")]
        public bool CreateConfigs { get; set; } = true;

        [BindProperty]
        [Display(Name = "重复轴处理方式")]
        public ZhouParserDuplicateCheckBehavior DuplicateCheckBehavior { get; set; }
            = ZhouParserDuplicateCheckBehavior.Error;

        private async Task<int?> CheckUserPrivilege()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return null;
            }
            var user = await _userManager.GetUserAsync(User);
            if (user is null || !user.GuildID.HasValue || !user.IsGuildAdmin)
            {
                return null;
            }
            return user.GuildID;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var guildID = await CheckUserPrivilege();
            if (!guildID.HasValue)
            {
                return RedirectToPage("/Guild/Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var guildID = await CheckUserPrivilege();
            if (!guildID.HasValue)
            {
                return RedirectToPage("/Guild/Index");
            }

            if (string.IsNullOrEmpty(Input))
            {
                StatusMessage2 = "错误：输入为空。";
                return Page();
            }

            var parser = _parserFactory.GetParser(_context, guildID.Value);
            await parser.ReadDatabase();

            var list = new List<Zhou>();
            var newConfigList = new List<CharacterConfig>();
            int lineNum = 0;
            var errorMsg = new StringBuilder();
            errorMsg.AppendLine("错误：读取轴表失败。");
            var errorCount = 0;
            const int MaxErrorMsg = 5;

            var allLines = DuplicateCheckBehavior == ZhouParserDuplicateCheckBehavior.Return ? new List<string>() : null;
            using var reader = new StringReader(Input);
            string line;
            while ((line = reader.ReadLine()) is not null)
            {
                allLines?.Add(line);
                lineNum += 1;
                try
                {
                    var z = parser.Parse(line, HasName, CreateConfigs, newConfigList);
                    if (z is not null)
                    {
                        list.Add(z);
                    }
                }
                catch (Exception e)
                {
                    if (++errorCount <= MaxErrorMsg)
                    {
                        errorMsg.AppendLine($"  第{lineNum}行：{e.Message}。");
                    }
                    list.Add(null);
                }
            }

            if (errorCount != 0)
            {
                if (errorCount > MaxErrorMsg)
                {
                    errorMsg.AppendLine($"  以及{errorCount - MaxErrorMsg}个其他错误。");
                }
                errorMsg.AppendLine("轴表没有被修改。");
                StatusMessage2 = errorMsg.ToString();
                return Page();
            }
            errorMsg.Clear();
            errorMsg.AppendLine("检测到重复的轴：");

            var unsavedMergeCheck = new List<Zhou>();
            var unsavedDupCheck = new List<ZhouVariant>();
            var newUserConfigs = new List<UserCharacterConfig>();
            var returnInputContent = new StringBuilder();
            var updatedZV = new HashSet<ZhouVariant>(); //Each zv can only be updated once. Use this to check.
            var dupUpdateCount = 0;

            foreach (var cc in newConfigList)
            {
                await Guilds.ConfigsModel.CheckAndAddRankConfigAsync(_context, cc, newUserConfigs);
            }

            for (int i = 0; i < list.Count; ++i)
            {
                var z = list[i];
                if (z is null) continue; //i is to keep the line number. If parsing fails it will be null.

                var v = ((List<ZhouVariant>)z.Variants)[0];

                Zhou existingSameZhou = null;
                if (Merge || DuplicateCheckBehavior != ZhouParserDuplicateCheckBehavior.Ignore)
                {
                    existingSameZhou = await _context.Zhous
                        .FirstOrDefaultAsync(zz =>
                            zz.GuildID == guildID.Value &&
                            zz.BossID == z.BossID &&
                            zz.C1ID == z.C1ID &&
                            zz.C2ID == z.C2ID &&
                            zz.C3ID == z.C3ID &&
                            zz.C4ID == z.C4ID &&
                            zz.C5ID == z.C5ID); //Assuming same order (by range).
                    if (existingSameZhou is null)
                    {
                        existingSameZhou = unsavedMergeCheck.FirstOrDefault(zz =>
                            zz.BossID == z.BossID &&
                            zz.C1ID == z.C1ID &&
                            zz.C2ID == z.C2ID &&
                            zz.C3ID == z.C3ID &&
                            zz.C4ID == z.C4ID &&
                            zz.C5ID == z.C5ID);
                    }
                }

                //Dup check.
                if (DuplicateCheckBehavior != ZhouParserDuplicateCheckBehavior.Ignore && existingSameZhou is not null)
                {
                    if (!HasName || existingSameZhou.Name == z.Name)
                    {
                        //Name check passed.
                        //Check configs.
                        if (existingSameZhou.ZhouID != 0)
                        {
                            await _context.Entry(existingSameZhou).Collection(zz => zz.Variants).LoadAsync();
                        }

                        bool dupCheckResult = true;
                        foreach (var existingV in existingSameZhou.Variants)
                        {
                            if (existingV.ZhouVariantID != 0)
                            {
                                await _context.Entry(existingV).Collection(vv => vv.CharacterConfigs).LoadAsync();
                            }

                            if (existingV.CharacterConfigs.Count != v.CharacterConfigs.Count)
                            {
                                continue;
                            }
                            bool checkFailed = false;
                            foreach (var existingConfig in existingV.CharacterConfigs)
                            {
                                if (existingConfig.CharacterConfigID == 0)
                                {
                                    //This is a new zv. Compare cc instance.
                                    if (!v.CharacterConfigs.Any(vcc => vcc.CharacterConfig == existingConfig.CharacterConfig))
                                    {
                                        checkFailed = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    //This is a db zv. Compare cc id.
                                    if (!v.CharacterConfigs.Any(vcc => vcc.CharacterConfigID == existingConfig.CharacterConfigID))
                                    {
                                        checkFailed = true;
                                        break;
                                    }
                                }
                            }
                            if (!checkFailed)
                            {
                                switch (DuplicateCheckBehavior)
                                {
                                case ZhouParserDuplicateCheckBehavior.Error:
                                    dupCheckResult = false;
                                    errorCount += 1;
                                    errorMsg.AppendLine($"  第{i + 1}行：检测到重复的轴。");
                                    break;
                                case ZhouParserDuplicateCheckBehavior.Modify:
                                    if (existingV.ZhouVariantID == 0 || updatedZV.Contains(existingV))
                                    {
                                        errorCount += 1;
                                        errorMsg.AppendLine($"  第{i + 1}行：输入中包含重复的轴，无法更新。");
                                    }
                                    else
                                    {
                                        existingV.CharacterConfigs.Clear();
                                        foreach (var ncc in v.CharacterConfigs)
                                        {
                                            existingV.CharacterConfigs.Add(ncc);
                                        }
                                        existingV.Content = v.Content;
                                        existingV.Damage = v.Damage;
                                        existingV.IsDraft = AsDraft;
                                        existingV.Name = v.Name; //This should be null.
                                        updatedZV.Add(existingV);
                                    }
                                    dupCheckResult = false;
                                    break;
                                case ZhouParserDuplicateCheckBehavior.Return:
                                    returnInputContent.AppendLine(allLines[i]);
                                    dupCheckResult = false;
                                    break;
                                }
                                break;
                            }
                        }
                        if (!dupCheckResult)
                        {
                            //Process next.
                            continue;
                        }
                    }
                }

                //Merge check.
                Zhou mergedInto = null;
                if (Merge)
                {
                    mergedInto = existingSameZhou;
                    if (HasName && mergedInto.Name != z.Name)
                    {
                        //Only merge when name is the same.
                        mergedInto = null;
                    }
                }

                //Add the new item.
                v.IsDraft = AsDraft;
                if (mergedInto is not null)
                {
                    v.ZhouID = mergedInto.ZhouID;
                    v.Zhou = mergedInto;
                    _context.ZhouVariants.Add(v);
                    await EditModel.CheckAndAddUserVariants(_context, guildID.Value, mergedInto, v, v.CharacterConfigs, newUserConfigs);
                }
                else
                {
                    _context.Zhous.Add(z);
                    await EditModel.CheckAndAddUserVariants(_context, guildID.Value, z, v, v.CharacterConfigs, newUserConfigs);
                    unsavedMergeCheck.Add(z);
                }
            }

            if (errorCount != 0)
            {
                //Error or modify.
                //Modify can also generate error (cannot modify a new zv).
                errorMsg.AppendLine("轴表没有被修改。");
                StatusMessage2 = errorMsg.ToString();
                return Page();
            }

            await _context.SaveChangesAsync();

            if (DuplicateCheckBehavior == ZhouParserDuplicateCheckBehavior.Return)
            {
                StatusMessage2 = "轴表已被修改。重复的项目如下，这些项目未被添加。";
                Input = returnInputContent.ToString();
                return Page();
            }

            StatusMessage = "导入成功。";
            if (dupUpdateCount > 0)
            {
                StatusMessage += $"{dupUpdateCount}个已存在的重复轴被更新。";
            }
            return RedirectToPage();
        }
    }
}
