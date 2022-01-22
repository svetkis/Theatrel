using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

namespace theatrel.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private ITgBotProcessor _tLBotProcessor;
    //private GarbageCollectorEventListener _listener;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        Bootstrapper.Start();
        await base.StartAsync(cancellationToken);

        Trace.Listeners.Add(new Trace2StdoutLogger());

        //_listener = new GarbageCollectorEventListener();

        Trace.TraceInformation("Worker.StartAsync");

        var timeZoneService = Bootstrapper.Resolve<ITimeZoneService>();
        timeZoneService.TimeZone = TimeZoneInfo.CreateCustomTimeZone("Moscow Time", new TimeSpan(03, 00, 00),
            "(GMT+03:00) Moscow Time", "Moscow Time");

        await Bootstrapper.Resolve<IDbService>().MigrateDb(cancellationToken);

        _tLBotProcessor = Bootstrapper.Resolve<ITgBotProcessor>();
        var tlBotService = Bootstrapper.Resolve<ITgBotService>();
        _tLBotProcessor.Start(tlBotService, cancellationToken);

        MemoryHelper.LogMemoryUsage();

        await ScheduleDataUpdates(cancellationToken);
        await ScheduleMichailovskyDataUpdates(cancellationToken);
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
        string upgradeJobCron = Environment.GetEnvironmentVariable("MariinskyJobSchedule");
        if (string.IsNullOrWhiteSpace(upgradeJobCron))
        {
            _logger.LogInformation("UpdateJobSchedule not found");
            return;
        }

        string group = "updateJob1Group";

        ISchedulerFactory schedulerFactory = new StdSchedulerFactory();

        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.Start(cancellationToken);

        IJobDetail job = JobBuilder.Create<UpdateMariinskyJob>()
            .WithIdentity("UpdateMariinskyJob", group)
            .Build();

        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity("updateJob1Trigger", group)
            .WithCronSchedule(upgradeJobCron, cron => { cron.InTimeZone(Bootstrapper.Resolve<ITimeZoneService>().TimeZone); })
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken);

        _logger.LogInformation($"UpdateMariinskyJob {upgradeJobCron} was scheduled");
    }

    private async Task ScheduleMichailovskyDataUpdates(CancellationToken cancellationToken)
    {
        string upgradeJobCron = Environment.GetEnvironmentVariable("MichailovskyJobSchedule");
        if (string.IsNullOrWhiteSpace(upgradeJobCron))
        {
            _logger.LogInformation("UpdateJobSchedule not found");
            return;
        }

        string group = "updateJob2Group";

        ISchedulerFactory schedulerFactory = new StdSchedulerFactory();

        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.Start(cancellationToken);

        IJobDetail job = JobBuilder.Create<UpdateMichailovskyJob>()
            .WithIdentity("UpdateMichailovskyJob", group)
            .Build();

        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity("updateJob2Trigger", group)
            .WithCronSchedule(upgradeJobCron, cron => { cron.InTimeZone(Bootstrapper.Resolve<ITimeZoneService>().TimeZone); })
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken);

        _logger.LogInformation($"UpdateMichailovskyJob {upgradeJobCron} was scheduled");
    }

    private async Task ScheduleOneTimeDataUpdate(CancellationToken cancellationToken)
    {
        string group = "updateJobGroup2";

        ISchedulerFactory schedulerFactory = new StdSchedulerFactory();

        IScheduler scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.Start(cancellationToken);

        IJobDetail job = JobBuilder.Create<UpdateJobBase>()
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