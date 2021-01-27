using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel
{
    internal static class TimeZoneHelper
    {
        private const string TimeZoneName = "China Standard Time";
        private static TimeZoneInfo _beijing = TimeZoneInfo.CreateCustomTimeZone(TimeZoneName,
            TimeSpan.FromHours(8), TimeZoneName, TimeZoneName);

        public static DateTime ToBeijingTime(DateTime utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utc, _beijing);
        }

        public static DateTime GetGameDate(DateTime beijingTime)
        {
            if (beijingTime.Date == DateTime.MinValue)
            {
                return beijingTime.Date;
            }
            return beijingTime.Date - (beijingTime.TimeOfDay < TimeSpan.FromHours(5) ? TimeSpan.FromDays(1) : default);
        }

        public static DateTime BeijingNow => ToBeijingTime(DateTime.UtcNow);

        public static DateTime GameTimeToday => GetGameDate(BeijingNow);
    }
}
