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

        public static async Task RunAllAsync(InMemoryStorageContext context)
        {
            var bosses = await BossIDConverter.Create(context.DbContext);
            var allGuilds = await context.DbContext.Guilds.Include(g => g.Members).ToListAsync();
            foreach (var g in allGuilds)
            {
                var imGuild = await context.GetGuild(g.GuildID);
                try
                {
                    //TODO should each guild update use a separate db context?
                    //(this depends on how the website is used)
                    if (await RunGuildAsync(context, g, imGuild, bosses))
                    {
                        g.LastYobotSync = TimeZoneHelper.BeijingNow;

                        //Update values for all combos (in the list).
                        await CalcComboValues.RunAllAsync(context.DbContext, g, null);

                        //Ensure each guild is saved separately.
                        await context.DbContext.SaveChangesAsync();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        public static async Task RunSingleAsync(InMemoryStorageContext context, Guild g, InMemoryGuild imGuild, bool forceRecalc)
        {
            var bosses = await BossIDConverter.Create(context.DbContext);
            var allGuilds = await context.DbContext.Guilds.Include(g => g.Members).ToListAsync();
            try
            {
                if (await RunGuildAsync(context, g, imGuild, bosses) || forceRecalc)
                {
                    g.LastYobotSync = TimeZoneHelper.BeijingNow;

                    //Update values for all combos (in the list).
                    await CalcComboValues.RunAllAsync(context.DbContext, g, null);

                    //Ensure each guild is saved separately.
                    await context.DbContext.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task<bool> RunGuildAsync(InMemoryStorageContext context, Guild guild, InMemoryGuild imGuild,
            BossIDConverter bosses)
        {
            if (string.IsNullOrEmpty(guild.YobotAPI)) return false;
            var data = await _client.GetFromJsonAsync<YobotData>(guild.YobotAPI, _jsonOptions);

            var gameToday = TimeZoneHelper.GameTimeToday;
            var todayData = data.Challenges
                .Where(c => c.ChallengeDate == gameToday)
                .GroupBy(c => c.QQID)
                .ToDictionary(c => c.Key, c => ProcessDailyData(c));

            var comboCalculator = new FindAllCombos();

            bool guildChanged = false;
            foreach (var u in guild.Members)
            {
                var imUser = imGuild.GetUserById(u.Id);
                var userChanged = false;

                if (u.QQID != 0 && !u.DisableYobotSync)
                {
                    List<UserCharacterStatus> userUsedCharacterList = await context.DbContext.UserCharacterStatuses
                        .Where(s => s.UserID == u.Id)
                        .ToListAsync(); ;

                    if (!todayData.TryGetValue(u.QQID, out var userData))
                    {
                        userData = Array.Empty<YobotChallenge>();
                    }

                    if (u.Attempts > userData.Length)
                    {
                        if (TimeZoneHelper.GetGameDate(u.LastConfirm) == gameToday)
                        {
                            //Can't decide. Ignore.
                            u.IsIgnored = true;

                            //Must update u.Attempts. Otherwise this user won't be properly reset in the following day.
                            u.Attempts = userData.Length;

                            //Still perform a guild update, and more importantly, save the above change.
                            guildChanged = true;

                            continue;
                        }

                        //The user has more attempts than bot reports. They are probably from yesterday.
                        //TODO this is reported to be broken
                        ClearUserAttempts(context.DbContext, u);
                        userChanged = true;
                    }

                    //Don't use else if. ClearUserAttempts will reset u.Attempts.
                    if (u.Attempts < userData.Length)
                    {
                        //u.Attempts + imUser.ComboZhouCount != 3 is possible when user selected a combo, and
                        //then manually updated used characters without recalculating combos.
                        if (imUser.SelectedComboIndex == -1 || u.Attempts + imUser.ComboZhouCount != 3)
                        {
                            u.IsIgnored = true;
                            u.Attempts = userData.Length; //See comments above.
                            continue;
                        }

                        //Prepare for the zhou mask.
                        var availableZhouMask = imUser.ComboZhouCount switch
                        {
                            1 => 1,
                            2 => 3,
                            3 => 7,
                            _ => 0,
                        };

                        var originalUserUsedCharacterCount = userUsedCharacterList.Count;
                        var originalUserGuessedAttempts = u.GuessedAttempts;
                        userChanged = true;

                        for (int i = u.Attempts; i < userData.Length; ++i)
                        {
                            AddUserAttemptAsync(u, userData[i], bosses, 
                                imUser.GetCombo(imUser.SelectedComboIndex), imUser.SelectedComboZhouIndex,
                                ref availableZhouMask, userUsedCharacterList);
                        }

                        if (u.IsIgnored)
                        {
                            //There are some changes we have made to the user that needs to be reverted. We could
                            //avoid modifying the db entry (like the mask), but this one is easy to revert so
                            //we keep it simple.
                            if (imUser.ComboZhouCount >= 3) u.Attempt1ID = null;
                            if (imUser.ComboZhouCount >= 2) u.Attempt2ID = null;
                            if (imUser.ComboZhouCount >= 1) u.Attempt3ID = null;
                            u.GuessedAttempts = originalUserGuessedAttempts;
                        }
                        else
                        {
                            //Now it's safe to modify the state of the user.
                            for (int i = originalUserUsedCharacterCount; i < userUsedCharacterList.Count; ++i)
                            {
                                context.DbContext.UserCharacterStatuses.Add(userUsedCharacterList[i]);
                            }
                        }
                    }

                    if (userChanged)
                    {
                        //If we reach here, userUsedCharacterList must not be null.
                        InheritCombo.ComboInheritInfo inheritComboInfo = null;
                        var userUsedCharacterSet = userUsedCharacterList.Select(cs => cs.CharacterID).ToHashSet();
                        if (imUser.SelectedComboIndex != -1)
                        {
                            //should get from IM context instead of DB context
                            inheritComboInfo = await InheritCombo.GetInheritInfo(context.DbContext, u, userUsedCharacterSet);
                        }
                        comboCalculator.Run(imUser, userUsedCharacterSet, 3 - u.Attempts, inheritComboInfo, u.ComboIncludesDrafts);
                        guildChanged = true;
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

        private static void ClearUserAttempts(ApplicationDbContext context, PcrIdentityUser user)
        {
            user.Attempts = 0;
            user.GuessedAttempts = 0;
            user.Attempt1ID = user.Attempt2ID = user.Attempt3ID = null;
            user.Attempt1Borrow = user.Attempt2Borrow = user.Attempt3Borrow = null;
            user.IsIgnored = false;

            context.UserCharacterStatuses.RemoveRange(context.UserCharacterStatuses
                .Where(s => s.UserID == user.Id));
        }

        //Decide which variant in the old selected combo is the one reported by yobot.
        private static int? DecideNewAttempt(InMemoryUser.Combo selectedCombo, int selectedZhou, int yobotBossID, ref int mask)
        {
            int? ret = null;

            //Check selected first.
            switch (selectedZhou)
            {
            case 0:
                if (yobotBossID == selectedCombo.GetZhouVariant(0).BossID && (mask & 1) != 0)
                {
                    mask -= 1;
                    ret = 0;
                }
                break;
            case 1:
                if (yobotBossID == selectedCombo.GetZhouVariant(1).BossID && (mask & 2) != 0)
                {
                    mask -= 2;
                    ret = 1;
                }
                break;
            case 2:
                if (yobotBossID == selectedCombo.GetZhouVariant(2).BossID && (mask & 4) != 0)
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

            //Then check others (must only have one matching).
            if (yobotBossID == selectedCombo.GetZhouVariant(0).BossID)
            {
                ret = 0;
            }
            if (yobotBossID == selectedCombo.GetZhouVariant(1).BossID)
            {
                ret = ret.HasValue ? -1 : 1;
            }
            if (yobotBossID == selectedCombo.GetZhouVariant(2).BossID)
            {
                ret = ret.HasValue ? -1 : 2;
            }
            return ret == -1 ? null : ret;
        }

        //TODO here we can be smarter: instead of only matching boss id, we can add a check of
        //used characters to exclude some possibilities.
        private static void AddUserAttemptAsync(PcrIdentityUser user, YobotChallenge data,
            BossIDConverter bosses, InMemoryUser.Combo selectedCombo, int selectedZhou,
            ref int mask, List<UserCharacterStatus> userCharactersResult)
        {
            user.Attempts += 1;
            if (user.IsIgnored)
            {
                return;
            }

            var decided = DecideNewAttempt(selectedCombo, selectedZhou, bosses.Convert(data), ref mask);
            if (decided.HasValue)
            {
                AddUserAttemptDecidedAsync(user,
                    selectedCombo.GetZhouVariant(decided.Value),
                    selectedCombo.GetZhouBorrow(decided.Value).Value,
                    userCharactersResult);
            }
            else
            {
                user.IsIgnored = true;
            }
        }

        private static void AddUserAttemptDecidedAsync(PcrIdentityUser user,
            InMemoryZhouVariant imZhouVariant, int borrowIndex, List<UserCharacterStatus> userCharactersResult)
        {
            //Mark as guessed.
            user.GuessedAttempts += 1;

            //Set attempt item.
            //Note that if user later become ignored, we need to revert these changes. See
            //comments in RunGuildAsync.
            switch (user.Attempts)
            {
            case 1:
                user.Attempt1ID = imZhouVariant.ZhouVariantID;
                break;
            case 2:
                user.Attempt2ID = imZhouVariant.ZhouVariantID;
                break;
            case 3:
                user.Attempt3ID = imZhouVariant.ZhouVariantID;
                break;
            }

            //Add characters.
            for (int i = 0; i < 5; ++i)
            {
                if (borrowIndex == i) continue;

                //No more check for conflicts. If we need to check we need to check earlier.
                //Also showing the same character twice in used list can warn user that
                //something is wrong.
                var newStatus = new UserCharacterStatus
                {
                    UserID = user.Id,
                    CharacterID = imZhouVariant.CharacterIDs[i],
                    IsUsed = true,
                };
                //Only add to the temporary list without modifying database. The user might
                //become ignored before we finish matching all attempts. If that happen, we
                //must leave the state of the user unchanged.
                userCharactersResult.Add(newStatus);
            }
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
                var context = scope.ServiceProvider.GetService<InMemoryStorageContext>();
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
