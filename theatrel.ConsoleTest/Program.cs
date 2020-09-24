using JetBrains.Profiler.Api;
using Quartz;
using Quartz.Impl;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Common;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TimeZoneService;
using theatrel.TLBot.Interfaces;

namespace theatrel.ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
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
            tlBotService.OnMessage += (sender, message) =>
            {
                GC.Collect();
                MemoryProfiler.GetSnapshot("OnMessage");
            };
            tLBotProcessor.Start(tlBotService, cts.Token);


             for (int i = 0; i < 1; ++i)
             {
                 Trace.TraceInformation("Before UpdatePlaybill");
                 GC.Collect();
                 MemoryProfiler.GetSnapshot("Before UpdatePlaybill");

                 var job = new UpdateJob();

                 if (!await job.UpdatePlaybill(cts.Token))
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

                await Task.Delay(1000);

                 GC.Collect();
                 MemoryProfiler.GetSnapshot("Update finished");
             }
            //await ScheduleOneTimeDataUpdate(CancellationToken.None);

            while (true)
            {
                await Task.Delay(10000, cts.Token);
            }

            GC.Collect();
            MemoryProfiler.GetSnapshot();

            Bootstrapper.Stop();
        }

        private async Task ScheduleDataUpdates(CancellationToken cancellationToken)
        {
            string upgradeJobCron = Environment.GetEnvironmentVariable("UpdateJobSchedule");
            if (string.IsNullOrWhiteSpace(upgradeJobCron))
            {
                Trace.TraceInformation("UpdateJobSchedule not found");
                return;
            }

            string group = "updateJobGroup";

            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();

            IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
            await scheduler.Start(cancellationToken);

            IJobDetail job = JobBuilder.Create<UpdateJob>()
                .WithIdentity("updateJob", group)
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("updateJobTrigger", group)
                .WithCronSchedule(upgradeJobCron, cron => { cron.InTimeZone(Bootstrapper.Resolve<ITimeZoneService>().TimeZone); })
                .Build();

            await scheduler.ScheduleJob(job, trigger, cancellationToken);

            Trace.TraceInformation($"Update job {upgradeJobCron} was scheduled");
        }

        private static async Task ScheduleOneTimeDataUpdate(CancellationToken cancellationToken)
        {
            string group = "updateJobGroup2";

            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();

            IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
            await scheduler.Start(cancellationToken);

            IJobDetail job = JobBuilder.Create<UpdateJob>()
                .WithIdentity("updateJobOnce", group)
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("updateJobOnceTrigger", group)
                .StartNow()
                .Build();

            await scheduler.ScheduleJob(job, trigger, cancellationToken);

            Trace.TraceInformation($"Update job once was scheduled");
        }

    }
}
