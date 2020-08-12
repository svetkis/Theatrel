using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using theatrel.DataAccess;
using theatrel.Interfaces;
using theatrel.TLBot.Interfaces;

namespace theatrel.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private ITLBotProcessor _tLBotProcessor;

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

            await Bootstrapper.Resolve<AppDbContext>().Database.MigrateAsync(cancellationToken);

            _tLBotProcessor = Bootstrapper.Resolve<ITLBotProcessor>();
            _tLBotProcessor.Start(Bootstrapper.Resolve<ITLBotService>(), cancellationToken);

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
                //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task ScheduleDataUpdates(CancellationToken cancellationToken)
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();

            IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
            await scheduler.Start(cancellationToken);
            _logger.LogInformation("Scheduler started");

            IJobDetail job = JobBuilder.Create<UpdateSeptemberJob>()
                .WithIdentity("job1", "group1")
                .Build();

            TimeZoneInfo moscowCustomTimeZone = TimeZoneInfo.CreateCustomTimeZone("Moscow Time", new TimeSpan(03, 00, 00), "(GMT+03:00) Moscow Time", "Moscow Time");

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("trigger1", "group1")
                .WithCronSchedule("0 10 10-20 * * ?", cron => { cron.InTimeZone(moscowCustomTimeZone); })
                .Build();

            await scheduler.ScheduleJob(job, trigger, cancellationToken);

            _logger.LogInformation("Job was scheduled");
        }

        public class UpdateSeptemberJob : IJob
        {
            public async Task Execute(IJobExecutionContext context)
            {
                Trace.TraceInformation("UpdateJob was started");
                try
                {
                    var updater = Bootstrapper.Resolve<IDataUpdater>();
                    await updater.UpdateAsync(1, new DateTime(2020, 9, 1), new DateTime(2020, 10, 1),
                        context.CancellationToken);

                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Job failed {ex.Message}");
                }

                Trace.TraceInformation("UpdateJob was finished");
            }
        }
    }
}
