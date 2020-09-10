using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Profiler.Api;
using Microsoft.EntityFrameworkCore;
using theatrel.Common;
using theatrel.DataAccess;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.DataUpdater;
using theatrel.TLBot.Interfaces;

namespace theatrel.ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Trace.Listeners.Add(new Trace2StdoutLogger());

            Bootstrapper.Start();

            GC.Collect();
            MemoryProfiler.GetSnapshot();

            var dbService = Bootstrapper.Resolve<IDbService>();
            await using (AppDbContext dbContext = dbService.GetDbContext())
            {
                await dbContext.Database.MigrateAsync();
            }

            var tLBotProcessor = Bootstrapper.Resolve<ITgBotProcessor>();
            var tlBotService = Bootstrapper.Resolve<ITgBotService>();
            tLBotProcessor.Start(tlBotService, CancellationToken.None);


            var updater = Bootstrapper.Resolve<IDbPlaybillUpdater>();

            await updater.UpdateAsync(1, new DateTime(2020, 09, 1), new DateTime(2020, 09, 30), CancellationToken.None);
            await updater.UpdateAsync(1, new DateTime(2020, 10, 1), new DateTime(2020, 10, 31), CancellationToken.None);
            await SubscriptionsTest.Test();

            GC.Collect();
            MemoryProfiler.GetSnapshot();

            await Task.Delay(1000);
            Bootstrapper.Stop();
        }
    }
}
