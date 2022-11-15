using JetBrains.Profiler.Api;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Common;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.Subscriptions;
using theatrel.Interfaces.TimeZoneService;
using theatrel.TLBot.Interfaces;

namespace theatrel.ConsoleTest;

internal static class Program
{
    public static async Task Main()
    {
        Trace.Listeners.Add(new Trace2StdoutLogger());

        //var listener = new GarbageCollectorEventListener();

        Bootstrapper.Start();

        var timeZoneService = Bootstrapper.Resolve<ITimeZoneService>();
        timeZoneService.TimeZone = TimeZoneInfo.CreateCustomTimeZone("Moscow Time", new TimeSpan(03, 00, 00),
            "(GMT+03:00) Moscow Time", "Moscow Time");

        CancellationTokenSource cts = new CancellationTokenSource();

        await Bootstrapper.Resolve<IDbService>().MigrateDb(cts.Token);

        MemoryProfiler.CollectAllocations(true);
        MemoryProfiler.GetSnapshot("1");

        var tLBotProcessor = Bootstrapper.Resolve<ITgBotProcessor>();
        var tlBotService = Bootstrapper.Resolve<ITgBotService>();

        tLBotProcessor.Start(tlBotService, cts.Token);

        var job = new UpdateJob();

        if (!await job.UpdateMariinskiPlaybill(cts.Token))
            return;

        MemoryProfiler.GetSnapshot("");

        if (!await job.UpdateMichailovskyPlaybill(cts.Token))
            return;

        Trace.TraceInformation("Before ProcessSubscriptions");
        //MemoryProfiler.GetSnapshot("Before ProcessSubscriptions");

        if (!await job.ProcessSubscriptions(cts.Token))
            return;

        Trace.TraceInformation("Before SubscriptionsCleanup");
        //MemoryProfiler.GetSnapshot("Before SubscriptionsCleanup");

        if (!await job.SubscriptionsCleanup(cts.Token))
            return;

        Trace.TraceInformation("Before PlaybillCleanup");
        //MemoryProfiler.GetSnapshot("Before PlaybillCleanup");
        if (!await job.PlaybillCleanup(cts.Token))
            return;

        MemoryProfiler.GetSnapshot("Update finished");

        while (true)
        {
            await Task.Delay(1000000, cts.Token);
            MemoryProfiler.GetSnapshot("");
        }
    }
}