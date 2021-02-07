using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PcrBattleChannel.Data;
using PcrBattleChannel.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PcrBattleChannel.Algorithm
{
    public static class YobotSync
    {
        private static readonly List<YobotChallenge> _dailyDataBuilder = new();

        private static readonly HttpClient _client = new();

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = new YobotNamingPolicy(),
        };

        public class YobotNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name)
            {
                return Regex.Replace(name, "(.)([A-Z][a-z])", "$1_$2").ToLower().Replace("remain", "ramain");
            }
        }

        public class YobotData
        {
            public int APIVersion { get; init; }
            public int Code { get; init; }
            public YobotGroupInfo GroupInfo { get; init; }
            public List<YobotMember> Members { get; init; } = new();
            public List<YobotChallenge> Challenges { get; init; } = new();
        }

        public class YobotGroupInfo
        {
            public int BattleId { get; init; }
            public string GameServer { get; init; }
            public ulong GroupId { get; init; }
            public string GroupName { get; init; }
        }

        public class YobotMember
        {
            public string Nickname { get; init; }
            public ulong QQID { get; init; }
        }

        public class YobotChallenge
        {
            public int BattleId { get; init; }
            public ulong? Behalf { get; init; }
            public int BossNum { get; init; }
            public int ChallengePcrdate { get; init; }
            public int ChallengePcrtime { get; init; }
            public uint ChallengeTime { get; init; }
            public int Cycle { get; init; }
            public int Damage { get; init; }
            public int HealthRemain { get; init; }
            public bool IsContinue { get; init; }
            public string Message { get; init; }
            public ulong QQID { get; init; }

            public DateTime ChallengeDate
            {
                get => new DateTime(1970, 1, 1) + TimeSpan.FromDays(ChallengePcrdate);
            }
        }

        private class BossIDConverter
        {
            private int[] _stageStartLaps;
            private List<(int id, int hp)>[] _bosses;

            public static async Task<BossIDConverter> Create(ApplicationDbContext context)
            {
                var stages = await context.BattleStages
                    .OrderBy(s => s.StartLap)
                    .ToListAsync();

                var ret = new List<(int id, int hp)>[stages.Count];
                for (int i = 0; i < stages.Count; ++i)
                {
                    ret[i] = new List<(int, int)>();
                }

                var bosses = await context.Bosses.ToListAsync();
                foreach (var boss in bosses)
                {
                    ret[stages.FindIndex(s => s.BattleStageID == boss.BattleStageID)].Add((boss.BossID, boss.Life));
                }
                return new()
                {
                    _bosses = ret,
                    _stageStartLaps = stages.Select(s => s.StartLap).ToArray(),
                };
            }

            public int Convert(YobotChallenge c)
            {
                for (int i = 1; i < _stageStartLaps.Length; ++i)
                {
                    if (_stageStartLaps[i] > c.Cycle - 1)
                    {
                        return _bosses[i - 1][c.BossNum - 1].id;
                    }
                }
                return _bosses[^1][c.BossNum - 1].id;
            }

            public (int bossIndex, int hp) ConvertToInfo(YobotChallenge c)
            {
                int ret = 0;
                for (int i = 1; i < _stageStartLaps.Length; ++i)
                {
                    if (_stageStartLaps[i] > c.Cycle - 1)
                    {
                        ret += (c.Cycle - 1 - _stageStartLaps[i - 1]) * _bosses[i - 1].Count;
                        return (ret + c.BossNum - 1, _bosses[i - 1][c.BossNum - 1].hp);
                    }
                    ret += (_stageStartLaps[i] - _stageStartLaps[i - 1]) * _bosses[i - 1].Count;
                }
                ret += (c.Cycle - 1 - _stageStartLaps[^1]) * _bosses[^1].Count;
                return (ret + c.BossNum - 1, _bosses[^1][c.BossNum - 1].hp);
            }
        }

        public static async Task RunAllAsync(ApplicationDbContext context)
        {
            var bosses = await BossIDConverter.Create(context);
            var allGuilds = await context.Guilds.Include(g => g.Members).ToListAsync();
            var comboList = new List<UserCombo>();
            foreach (var g in allGuilds)
            {
                try
                {
                    //TODO should each guild update use a separate db context?
                    //(this depends on how the website is used)
                    if (await RunGuildAsync(context, g, bosses, comboList))
                    {
                        g.LastYobotSync = TimeZoneHelper.BeijingNow;

                        //Update values for all combos (in the list).
                        await CalcComboValues.RunAllAsync(context, g, comboList);

                        //Ensure each guild is saved separately.
                        await context.SaveChangesAsync();
                    }
                    comboList.Clear();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public static async Task RunSingleAsync(ApplicationDbContext context, Guild g, bool forceRecalc)
        {
            var bosses = await BossIDConverter.Create(context);
            var allGuilds = await context.Guilds.Include(g => g.Members).ToListAsync();
            var comboList = new List<UserCombo>();
            try
            {
                if (await RunGuildAsync(context, g, bosses, comboList) || forceRecalc)
                {
                    g.LastYobotSync = TimeZoneHelper.BeijingNow;

                    //Update values for all combos (in the list).
                    await CalcComboValues.RunAllAsync(context, g, comboList);

                    //Ensure each guild is saved separately.
                    await context.SaveChangesAsync();
                }
                comboList.Clear();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task<bool> RunGuildAsync(ApplicationDbContext context, Guild guild, BossIDConverter bosses,
            List<UserCombo> comboListResult)
        {
            if (string.IsNullOrEmpty(guild.YobotAPI)) return false;
            var data = await _client.GetFromJsonAsync<YobotData>(guild.YobotAPI, _jsonOptions);

            var gameToday = TimeZoneHelper.GameTimeToday;
            var todayData = data.Challenges
                .Where(c => c.ChallengeDate == gameToday)
                .GroupBy(c => c.QQID)
                .ToDictionary(c => c.Key, c => ProcessDailyData(c));

            bool guildChanged = false;
            foreach (var u in guild.Members)
            {
                if (u.IsIgnored) continue;

                //Collect all combos
                var userCombos = await context.UserCombos
                    .Include(c => c.Zhou1)
                    .Include(c => c.Zhou2)
                    .Include(c => c.Zhou3)
                    .Where(c => c.UserID == u.Id)
                    .ToListAsync();
                var userChanged = false;
                List<UserCharacterStatus> userUsedCharacterList = null; //Lazy loading.

                if (u.QQID != 0 && !u.DisableYobotSync)
                {
                    if (!todayData.TryGetValue(u.QQID, out var userData))
                    {
                        if (u.Attempts != 0)
                        {
                            ClearUserAttempts(context, u, ref userUsedCharacterList);
                            userChanged = true;
                        }
                    }
                    else
                    {
                        if (u.Attempts > userData.Length)
                        {
                            if (TimeZoneHelper.GetGameDate(u.LastConfirm) < gameToday)
                            {
                                //The user has more attempts than bot reports. They are probably from yesterday.
                                ClearUserAttempts(context, u, ref userUsedCharacterList);
                                userChanged = true;
                            }
                            else
                            {
                                //Can't decide. Ignore.
                                u.IsIgnored = true;

                                //Must update u.Attempts. Otherwise this user won't be properly reset in the following day.
                                u.Attempts = userData.Length;

                                //Still perform a guild update, and more importantly, save the above change.
                                guildChanged = true;

                                continue;
                            }
                        }

                        //Don't use else if. ClearUserAttempts will reset u.Attempts.
                        if (u.Attempts < userData.Length)
                        {
                            var selectedCombo = userCombos.FirstOrDefault(c => c.SelectedZhou.HasValue);
                            if (selectedCombo is null)
                            {
                                u.IsIgnored = true;
                                u.Attempts = userData.Length; //See comments above.
                                continue;
                            }

                            //Prepare for the zhou mask.
                            int availableZhouCountInCombo = 0, availableZhouMask = 0;
                            if (selectedCombo.Zhou1ID.HasValue)
                            {
                                availableZhouCountInCombo += 1;
                                availableZhouMask += 1;
                            }
                            if (selectedCombo.Zhou2ID.HasValue)
                            {
                                availableZhouCountInCombo += 1;
                                availableZhouMask += 2;
                            }
                            if (selectedCombo.Zhou3ID.HasValue)
                            {
                                availableZhouCountInCombo += 1;
                                availableZhouMask += 4;
                            }
                            if (availableZhouCountInCombo + u.Attempts != 3)
                            {
                                u.IsIgnored = true;
                                u.Attempts = userData.Length; //See comments above.
                                continue;
                            }

                            userUsedCharacterList ??= await context.UserCharacterStatuses
                                .Where(s => s.UserID == u.Id)
                                .ToListAsync();
                            var originalUserUsedCharacterCount = userUsedCharacterList.Count;
                            var originalUserGuessedAttempts = u.GuessedAttempts;
                            userChanged = true;

                            for (int i = u.Attempts; i < userData.Length; ++i)
                            {
                                await AddUserAttemptAsync(context, u, userData[i], bosses, selectedCombo, ref availableZhouMask,
                                    userUsedCharacterList);
                            }

                            if (u.IsIgnored)
                            {
                                //There are some changes we have made to the user that needs to be reverted. We could
                                //avoid modifying the db entry (like the mask), but this one is easy to revert so
                                //we keep it simple.
                                if (availableZhouCountInCombo >= 3) u.Attempt1ID = null;
                                if (availableZhouCountInCombo >= 2) u.Attempt2ID = null;
                                if (availableZhouCountInCombo >= 1) u.Attempt3ID = null;
                                u.GuessedAttempts = originalUserGuessedAttempts;
                            }
                            else
                            {
                                //Now it's safe to modify the state of the user.
                                for (int i = originalUserUsedCharacterCount; i < userUsedCharacterList.Count; ++i)
                                {
                                    context.UserCharacterStatuses.Add(userUsedCharacterList[i]);
                                }
                            }
                        }
                    }
                }

                if (!u.IsIgnored)
                {
                    //Even if the user don't have QQID, we must still include the combos for optimization.
                    if (userChanged)
                    {
                        //If we reach here, userUsedCharacterList must not be null.
                        context.UserCombos.RemoveRange(userCombos);
                        await FindAllCombos.RunAsync(context, u, userUsedCharacterList, comboListResult, inherit: true);
                        guildChanged = true;
                    }
                    else
                    {
                        comboListResult.AddRange(userCombos);
                    }
                }
            }

            int newBossIndex;
            float newBossDamageRatio;
            if (data.Challenges.Count == 0)
            {
                newBossIndex = 0;
                newBossDamageRatio = 0;
            }
            else
            {
                var bossInfo = bosses.ConvertToInfo(data.Challenges[^1]);
                newBossIndex = bossInfo.bossIndex;
                newBossDamageRatio = 1 - data.Challenges[^1].HealthRemain / (float)bossInfo.hp;
            }
            if (guild.BossIndex != newBossIndex || MathF.Abs(guild.BossDamageRatio - newBossDamageRatio) > 0.0001f)
            {
                guild.BossIndex = newBossIndex;
                guild.BossDamageRatio = newBossDamageRatio;
                guildChanged = true;
            }

            return guildChanged;
        }

        //Simplify daily data by removing either one in continuation challange pairs.
        private static YobotChallenge[] ProcessDailyData(IEnumerable<YobotChallenge> input)
        {
            _dailyDataBuilder.Clear();
            YobotChallenge cont = null;

            foreach (var c in input)
            {
                if (cont is not null)
                {
                    _dailyDataBuilder.Add(cont.Damage > c.Damage ? cont : c);
                    cont = null;
                }
                else if (c.HealthRemain == 0 && !c.IsContinue)
                {
                    cont = c;
                }
                else
                {
                    _dailyDataBuilder.Add(c);
                }
            }
            if (cont is not null)
            {
                _dailyDataBuilder.Add(cont);
            }

            if (_dailyDataBuilder.Count > 3)
            {
                //Corrupted data from yobot. Don't proceed.
                throw new Exception();
            }
            return _dailyDataBuilder.ToArray();
        }

        private static void ClearUserAttempts(ApplicationDbContext context, PcrIdentityUser user,
            ref List<UserCharacterStatus> userUsedCharacterList)
        {
            user.Attempts = 0;
            user.GuessedAttempts = 0;
            user.Attempt1ID = user.Attempt2ID = user.Attempt3ID = null;
            user.Attempt1Borrow = user.Attempt2Borrow = user.Attempt3Borrow = null;
            user.IsIgnored = false;

            if (userUsedCharacterList is null)
            {
                context.UserCharacterStatuses.RemoveRange(context.UserCharacterStatuses
                    .Where(s => s.UserID == user.Id));
                userUsedCharacterList = new();
            }
            else
            {
                context.UserCharacterStatuses.RemoveRange(userUsedCharacterList);
                userUsedCharacterList.Clear();
            }
        }

        //Decide which variant in the old selected combo is the one reported by yobot.
        private static int? DecideNewAttempt(UserCombo selectedCombo, int yobotBossID, ref int mask)
        {
            int? ret = null;

            //Check selected first.
            if (selectedCombo.SelectedZhou.HasValue)
            {
                switch (selectedCombo.SelectedZhou)
                {
                case 0:
                    if (yobotBossID == selectedCombo.Boss1 && (mask & 1) != 0)
                    {
                        mask -= 1;
                        ret = 0;
                    }
                    break;
                case 1:
                    if (yobotBossID == selectedCombo.Boss2 && (mask & 2) != 0)
                    {
                        mask -= 2;
                        ret = 1;
                    }
                    break;
                case 2:
                    if (yobotBossID == selectedCombo.Boss3 && (mask & 4) != 0)
                    {
                        mask -= 4;
                        ret = 2;
                    }
                    break;
                }
                if (ret.HasValue)
                {
                    return ret;
                }
            }

            //Then check others (must only have one matching).
            if (yobotBossID == selectedCombo.Boss1)
            {
                ret = 0;
            }
            if (yobotBossID == selectedCombo.Boss2)
            {
                ret = ret.HasValue ? -1 : 1;
            }
            if (yobotBossID == selectedCombo.Boss3)
            {
                ret = ret.HasValue ? -1 : 2;
            }
            return ret;
        }

        //TODO here we can be smarter: instead of only matching boss id, we can add a check of
        //used characters to exclude some possibilities.
        private static Task AddUserAttemptAsync(ApplicationDbContext context, PcrIdentityUser user, YobotChallenge data,
            BossIDConverter bosses, UserCombo selectedCombo, ref int mask, List<UserCharacterStatus> userCharactersResult)
        {
            user.Attempts += 1;
            if (user.IsIgnored)
            {
                return Task.CompletedTask;
            }

            var borrowInfo = selectedCombo.BorrowInfo.Split(';')[0].Split(',');
            switch (DecideNewAttempt(selectedCombo, bosses.Convert(data), ref mask))
            {
            case 0:
                return AddUserAttemptDecidedAsync(context, user, selectedCombo.Zhou1, borrowInfo[0], userCharactersResult);
            case 1:
                return AddUserAttemptDecidedAsync(context, user, selectedCombo.Zhou2, borrowInfo[1], userCharactersResult);
            case 2:
                return AddUserAttemptDecidedAsync(context, user, selectedCombo.Zhou3, borrowInfo[2], userCharactersResult);
            default:
                user.IsIgnored = true;
                return Task.CompletedTask;
            }
        }

        private static async Task AddUserAttemptDecidedAsync(ApplicationDbContext context, PcrIdentityUser user,
            UserZhouVariant v, string borrowInfoElement, List<UserCharacterStatus> userCharactersResult)
        {
            void AddCharacter(int? cid)
            {
                if (!cid.HasValue) return;
                //No more check for conflicts. If we need to check we need to check earlier.
                //Also showing the same character twice in used list can warn user that
                //something is wrong.
                var newStatus = new UserCharacterStatus
                {
                    UserID = user.Id,
                    CharacterID = cid.Value,
                    IsUsed = true,
                };
                //Only add to the temporary list without modifying database. The user might
                //become ignored before we finish matching all attempts. If that happen, we
                //must leave the state of the user unchanged.
                userCharactersResult.Add(newStatus);
            }

            //Mark as guessed.
            user.GuessedAttempts += 1;

            //Set attempt item.
            //Note that if user later become ignored, we need to revert these changes. See
            //comments in RunGuildAsync.
            switch (user.Attempts)
            {
            case 1:
                user.Attempt1ID = v.ZhouVariantID;
                break;
            case 2:
                user.Attempt2ID = v.ZhouVariantID;
                break;
            case 3:
                user.Attempt3ID = v.ZhouVariantID;
                break;
            }

            //Add characters.
            int borrowIndex = int.Parse(borrowInfoElement);
            var zhou = await context.ZhouVariants
                .Where(zv => zv.ZhouVariantID == v.ZhouVariantID)
                .Include(zv => zv.Zhou)
                .Select(zv => zv.Zhou)
                .FirstAsync();
            if (borrowIndex != 0) AddCharacter(zhou.C1ID);
            if (borrowIndex != 1) AddCharacter(zhou.C2ID);
            if (borrowIndex != 2) AddCharacter(zhou.C3ID);
            if (borrowIndex != 3) AddCharacter(zhou.C4ID);
            if (borrowIndex != 4) AddCharacter(zhou.C5ID);
        }
    }

    public class YobotSyncScheduler : IHostedService
    {
        public static TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(10);
        public static DateTime UtcLastUpdateFinishTime { get; private set; }
        public static TimeSpan LastUpdateLength { get; private set; }

        private readonly SemaphoreSlim _lock = new(1, 1);
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private Timer _timer;

        public YobotSyncScheduler(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (Interval != TimeSpan.Zero)
            {
                _timer = new Timer(Run, null, Interval, Interval);
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _timer?.Dispose();
            return Task.CompletedTask;
        }

        private void Run(object state)
        {
            if (!_lock.Wait(0)) return;
            try
            {
                var timer = Stopwatch.StartNew();
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
                YobotSync.RunAllAsync(context).Wait();
                LastUpdateLength = timer.Elapsed;
                UtcLastUpdateFinishTime = DateTime.UtcNow;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
