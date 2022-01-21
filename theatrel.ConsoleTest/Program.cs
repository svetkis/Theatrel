using JetBrains.Profiler.Api;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Common;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TimeZoneService;
using theatrel.TLBot.Interfaces;

namespace theatrel.ConsoleTest;

class Program
{
    static async Task Main()
    {
        Trace.Listeners.Add(new Trace2StdoutLogger());

        Bootstrapper.Start();

        var timeZoneService = Bootstrapper.Resolve<ITimeZoneService>();
        timeZoneService.TimeZone = TimeZoneInfo.CreateCustomTimeZone("Moscow Time", new TimeSpan(03, 00, 00),
            "(GMT+03:00) Moscow Time", "Moscow Time");

        CancellationTokenSource cts = new CancellationTokenSource();

        await Bootstrapper.Resolve<IDbService>().MigrateDb(cts.Token);

        MemoryProfiler.CollectAllocations(true);

        var tLBotProcessor = Bootstrapper.Resolve<ITgBotProcessor>();
        var tlBotService = Bootstrapper.Resolve<ITgBotService>();

        tLBotProcessor.Start(tlBotService, cts.Token);

        for (int i = 0; i < 1; ++i)
        {
            Trace.TraceInformation("Before UpdateMariinskiPlaybill");
            GC.Collect();
            MemoryProfiler.GetSnapshot("Before UpdateMariinskiPlaybill");

            var job = new UpdateJob();

            if (!await job.UpdateMariinskiPlaybill(cts.Token))
                return;

            if (!await job.UpdateMichailovskyPlaybill(cts.Token))
                return;

            Trace.TraceInformation("Before ProcessSubscriptions");
            GC.Collect();
            MemoryProfiler.GetSnapshot("Before ProcessSubscriptions");

            if (!await job.ProcessSubscriptions(cts.Token))
                return;

            Trace.TraceInformation("Before SubscriptionsCleanup");
            GC.Collect();
            MemoryProfiler.GetSnapshot("Before SubscriptionsCleanup");

            if (!await job.SubscriptionsCleanup(cts.Token))
                return;

            if (!await job.PlaybillCleanup(cts.Token))
                return;

            GC.Collect();
            MemoryProfiler.GetSnapshot("Update finished");
        }
        //await ScheduleOneTimeDataUpdate(CancellationToken.None);

        while (true)
        {
            await Task.Delay(100000, cts.Token);
            GC.Collect();
            MemoryProfiler.GetSnapshot("");

        }

        //   Bootstrapper.Stop();
    }
}