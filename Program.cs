using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PcrBattleChannel.Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PcrBattleChannel
{
    public class Program
    {
        private static readonly Dictionary<string, Action<string>> _argHandlers = new()
        {
            { "admin", val => Areas.Identity.Pages.Account.RegisterModel.AdminEmail = val },
            { "yobot_sync", val => YobotSyncScheduler.Interval = TimeSpan.FromMinutes(double.Parse(val)) },
            { "allow_add_guild", val => Pages.Home.AddGuildModel.IsAllowed = true },
        };

        private static void HandleArgs(string[] args)
        {
            foreach (var arg in args.Select(aa => aa.Split('=')).GroupBy(aa => aa[0]))
            {
                if (!_argHandlers.TryGetValue(arg.Key, out var handler))
                {
                    throw new Exception($"Unknown argument {arg.Key}");
                }
                if (arg.Count() != 1)
                {
                    throw new Exception($"Duplicate argument {arg.Key}");
                }
                var val = arg.First();
                handler(val.Length switch
                {
                    1 => null,
                    2 => val[1],
                    _ => throw new Exception($"Invalid argument {arg.Key}"),
                });
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
        }

        public static void Main(string[] args)
        {
            try
            {
                HandleArgs(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            CreateHostBuilder(args).Build().Run();
        }
    }
}
