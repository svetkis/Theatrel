using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.DataUpdater;
using theatrel.Interfaces.Subscriptions;
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
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);

            _tLBotProcessor = Bootstrapper.Resolve<ITgBotProcessor>();
            var tlBotService = Bootstrapper.Resolve<ITgBotService>();
            _tLBotProcessor.Start(tlBotService, cancellationToken);

            await ScheduleDataUpdates(cancellationToken);
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
            string group2 = "updateJobOnceWhenStart";

            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();

            IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
            await scheduler.Start(cancellationToken);
            _logger.LogInformation("Scheduler started");

            IJobDetail job = JobBuilder.Create<UpdateSeptemberJob>()
                .WithIdentity("updateJob", group)
                .Build();

            IJobDetail startUpdateJob = JobBuilder.Create<UpdateSeptemberJob>()
                .WithIdentity("startUpdateJob", group)
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("updateJobTrigger", group)
                .WithCronSchedule("0 10 10-20 * * ?", cron => { cron.InTimeZone(Bootstrapper.Resolve<ITimeZoneService>().TimeZone); })
                .Build();

            ITrigger triggerNow = TriggerBuilder.Create()
                .WithIdentity("triggerNow", group2)
                .StartNow()
                .Build();

            await scheduler.ScheduleJob(job, trigger, cancellationToken);
            await scheduler.ScheduleJob(startUpdateJob, triggerNow, cancellationToken);

            _logger.LogInformation("Job was scheduled");
        }

        public class UpdateSeptemberJob : IJob
        {
            public async Task Execute(IJobExecutionContext context)
            {
                Trace.TraceInformation("UpdateJob was started");
                try
                {
                    ISubscriptionService subscriptionServices = Bootstrapper.Resolve<ISubscriptionService>();
                    var filters = subscriptionServices.GetUpdateFilters();

                    IDbPlaybillUpdater updater = Bootstrapper.Resolve<IDbPlaybillUpdater>();

                    foreach (var filter in filters)
                    {
                        await updater.UpdateAsync(1, filter.StartDate, filter.EndDate, context.CancellationToken);
                    }

                    ISubscriptionProcessor subscriptionProcessor = Bootstrapper.Resolve<ISubscriptionProcessor>();
                    await subscriptionProcessor.ProcessSubscriptions();
                }
                catch (Exception ex)
                {
                    if (long.TryParse(Environment.GetEnvironmentVariable("OwnerTelegramgId"), out var ownerId))
                    {
                        var telegramService = Bootstrapper.Resolve<ITgBotService>();
                        await telegramService.SendMessageAsync(ownerId, "UpdateJob failed");
                        await telegramService.SendMessageAsync(ownerId, $"{ex.Message}");
                        Trace.TraceError(ex.StackTrace);
                    }

                    Trace.TraceError($"Job failed {ex.Message}");
                }

                Trace.TraceInformation("UpdateJob was finished");
            }
        }
    }
}
