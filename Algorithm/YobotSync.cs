using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PcrBattleChannel.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PcrBattleChannel.Algorithm
{
    public static class YobotSync
    {
        public static async Task Run(ApplicationDbContext context)
        {
            await context.SaveChangesAsync();
        }
    }

    public class YobotSyncScheduler : IHostedService
    {
        public static TimeSpan Interval = TimeSpan.FromMinutes(10);

        private readonly SemaphoreSlim _lock = new(0, 1);
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private Timer _timer;

        public YobotSyncScheduler(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(Run, null, Interval, Interval);
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
                using var scope = _serviceScopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
                YobotSync.Run(context).Wait();
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
