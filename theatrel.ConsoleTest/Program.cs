using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Profiler.Api;
using Microsoft.EntityFrameworkCore;
using Quartz;
using Quartz.Impl;
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

            await using (var dbContext = Bootstrapper.Resolve<IDbService>().GetDbContext())
            {
                await dbContext.Database.MigrateAsync(CancellationToken.None);
            }

            var tLBotProcessor = Bootstrapper.Resolve<ITgBotProcessor>();
            var tlBotService = Bootstrapper.Resolve<ITgBotService>();
            tLBotProcessor.Start(tlBotService, CancellationToken.None);

            GC.Collect();
            MemoryProfiler.GetSnapshot("Before UpdatePlaybill");

            var job = new UpdateJob();

            if (!await job.UpdatePlaybill(CancellationToken.None))
                return;

            GC.Collect();
            MemoryProfiler.GetSnapshot("Before ProcessSubscriptions");

            if (!await job.ProcessSubscriptions(CancellationToken.None))
                return;

            GC.Collect();
            MemoryProfiler.GetSnapshot("Before SubscriptionsCleanup");

            if (!await job.SubscriptionsCleanup(CancellationToken.None))
                return;

            GC.Collect();
            MemoryProfiler.GetSnapshot("Update finished");


            GC.Collect();
            MemoryProfiler.GetSnapshot("Before UpdatePlaybill");

            if (!await job.UpdatePlaybill(CancellationToken.None))
                return;

            GC.Collect();
            MemoryProfiler.GetSnapshot("Before ProcessSubscriptions");

            if (!await job.ProcessSubscriptions(CancellationToken.None))
                return;

            GC.Collect();
            MemoryProfiler.GetSnapshot("Before SubscriptionsCleanup");

            if (!await job.SubscriptionsCleanup(CancellationToken.None))
                return;

            GC.Collect();
            MemoryProfiler.GetSnapshot("Update finished");

            //await ScheduleOneTimeDataUpdate(CancellationToken.None);

            while (true)
            {
                await Task.Delay(10000);
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
