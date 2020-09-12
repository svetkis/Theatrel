using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using theatrel.Common;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TimeZoneService;
using theatrel.TLBot.Interfaces;

namespace theatrel.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private ITgBotProcessor _tLBotProcessor;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            Bootstrapper.Start();
            await base.StartAsync(cancellationToken);

            Trace.Listeners.Add(new Trace2StdoutLogger());

            Trace.TraceInformation("Worker.StartAsync");

            var timeZoneService = Bootstrapper.Resolve<ITimeZoneService>();
            timeZoneService.TimeZone = TimeZoneInfo.CreateCustomTimeZone("Moscow Time", new TimeSpan(03, 00, 00),
                "(GMT+03:00) Moscow Time", "Moscow Time");

            await using var dbContext = Bootstrapper.Resolve<IDbService>().GetDbContext();
            await dbContext.Database.MigrateAsync(cancellationToken);

            _tLBotProcessor = Bootstrapper.Resolve<ITgBotProcessor>();
            var tlBotService = Bootstrapper.Resolve<ITgBotService>();
            _tLBotProcessor.Start(tlBotService, cancellationToken);

            MemoryHelper.LogMemoryUsage();

            await ScheduleDataUpdates(cancellationToken);
            await ScheduleOneTimeDataUpdate(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Trace.TraceInformation("Worker.StopAsync");

            _tLBotProcessor.Stop();
            Bootstrapper.Stop();

            await base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task ScheduleDataUpdates(CancellationToken cancellationToken)
        {
            string upgradeJobCron = Environment.GetEnvironmentVariable("UpdateJobSchedule");
            if (string.IsNullOrWhiteSpace(upgradeJobCron))
            {
                _logger.LogInformation("UpdateJobSchedule not found");
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

            _logger.LogInformation($"Update job {upgradeJobCron} was scheduled");
        }

        private async Task ScheduleOneTimeDataUpdate(CancellationToken cancellationToken)
        {
            string upgradeJobCron = Environment.GetEnvironmentVariable("UpdateJobSchedule");
            if (string.IsNullOrWhiteSpace(upgradeJobCron))
            {
                _logger.LogInformation("UpdateJobSchedule not found");
                return;
            }

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

            _logger.LogInformation($"Update job once was scheduled");
        }

    }
}
