using Autofac;
using Quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Profiler.Api;
using theatrel.Common;
using theatrel.Interfaces.DataUpdater;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Subscriptions;
using theatrel.TLBot.Interfaces;

namespace theatrel.ConsoleTest;

public class UpdateJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        Trace.TraceInformation("UpdateJob was started");

        if (!await UpdateMariinskiPlaybill(context.CancellationToken))
            return;

        if (!await UpdateMichailovskyPlaybill(context.CancellationToken))
            return;

        if (!await ProcessSubscriptions(context.CancellationToken))
            return;

        if (!await PlaybillCleanup(context.CancellationToken))
            return;

        if (!await SubscriptionsCleanup(context.CancellationToken))
            return;

        Trace.TraceInformation("UpdateJob was finished");
    }

    public async Task<bool> UpdateMariinskiPlaybill(CancellationToken cToken)
    {
        try
        {
            ISubscriptionService subscriptionServices = Bootstrapper.Resolve<ISubscriptionService>();
            IPerformanceFilter[] filters = subscriptionServices.GetUpdateFilters();

            var culture = CultureInfo.CreateSpecificCulture("ru");
            foreach (var range in GetDateRanges(6))
            {
                await using (var scope = Bootstrapper.RootScope.BeginLifetimeScope())
                {
                    IDbPlaybillUpdater updater = scope.Resolve<IDbPlaybillUpdater>();

                    Trace.TraceInformation($"Update playbill Mariinski for interval {range.Item1.ToString("d", culture)} {range.Item2.ToString("d", culture)}");
                    await updater.Update(1, range.Item1, range.Item2, cToken);
                }

                MemoryHelper.Collect(true);
                MemoryProfiler.GetSnapshot("");
            }

            return true;
        }
        catch (Exception ex)
        {
            await SendExceptionMessageToOwner("UpdateMariinskiPlaybill", ex);
            Trace.TraceError($"UpdateMariinskiPlaybill failed {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateMichailovskyPlaybill(CancellationToken cToken)
    {
        try
        {
            ISubscriptionService subscriptionServices = Bootstrapper.Resolve<ISubscriptionService>();
            IPerformanceFilter[] filters = subscriptionServices.GetUpdateFilters();

            var culture = CultureInfo.CreateSpecificCulture("ru");
            foreach (var range in GetDateRanges(6))
            {
                await using (var scope = Bootstrapper.RootScope.BeginLifetimeScope())
                {
                    IDbPlaybillUpdater updater = scope.Resolve<IDbPlaybillUpdater>();

                    Trace.TraceInformation($"Update Michailovsky playbill for interval {range.Item1.ToString("d", culture)} {range.Item2.ToString("d", culture)}");
                    await updater.Update(2, range.Item1, range.Item2, cToken);

                }

                MemoryHelper.Collect(true);
                MemoryProfiler.GetSnapshot("");
            }

            return true;
        }
        catch (Exception ex)
        {
            await SendExceptionMessageToOwner("UpdateMichailovskyPlaybill", ex);
            Trace.TraceError($"UpdateMichailovskyPlaybill failed {ex.Message}");
            return false;
        }
    }

    private static Tuple<DateTime, DateTime>[] GetDateRanges(int monthsCount)
    {
        List<Tuple<DateTime, DateTime>> dates = new List<Tuple<DateTime, DateTime>>();
        int currentMonth = DateTime.Now.Month;


        for (int month = currentMonth; month < currentMonth + monthsCount; ++month)
        {
            int m = month > 12 ? month % 12 : month;
            int y = month > 12 ? DateTime.UtcNow.Year + month / 12 : DateTime.UtcNow.Year;

            var date = new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);

            dates.Add(Tuple.Create(date, date.AddMonths(1)));
        }

        return dates.ToArray();
    }

    public async Task<bool> PlaybillCleanup(CancellationToken cToken)
    {
        try
        {
            Trace.TraceInformation("PlaybillCleanup CleanUpOutDatedSubscriptions");

            MemoryProfiler.GetSnapshot("Before PlaybillCleanup");

            await using var scope = Bootstrapper.RootScope.BeginLifetimeScope();
            IPlaybillCleanUpService cleanUpService = scope.Resolve<IPlaybillCleanUpService>();
            await cleanUpService.CleanUp();

            MemoryProfiler.GetSnapshot("After PlaybillCleanup");

            return true;
        }
        catch (Exception ex)
        {
            await SendExceptionMessageToOwner("PlaybillCleanup", ex);
            Trace.TraceError($"PlaybillCleanup failed {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SubscriptionsCleanup(CancellationToken cToken)
    {
        try
        {
            Trace.TraceInformation("Subscriptions CleanUpOutDatedSubscriptions");
            MemoryProfiler.GetSnapshot("Before CleanUpOutDatedSubscriptions");

            await using var scope = Bootstrapper.RootScope.BeginLifetimeScope();
            ISubscriptionsUpdaterService cleanUpService = scope.Resolve<ISubscriptionsUpdaterService>();
            await cleanUpService.CleanUpOutDatedSubscriptions();

            MemoryProfiler.GetSnapshot("After CleanUpOutDatedSubscriptions");

            return true;
        }
        catch (Exception ex)
        {
            await SendExceptionMessageToOwner("SubscriptionsCleanup", ex);
            Trace.TraceError($"SubscriptionsCleanup failed {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ProcessSubscriptions(CancellationToken cToken)
    {
        try
        {
            Trace.TraceInformation("ProcessSubscriptions");

            await using var scope = Bootstrapper.RootScope.BeginLifetimeScope();

            ISubscriptionProcessor subscriptionProcessor = scope.Resolve<ISubscriptionProcessor>();
            await subscriptionProcessor.ProcessSubscriptions();

            return true;
        }
        catch (Exception ex)
        {
            await SendExceptionMessageToOwner("ProcessSubscriptions", ex);
            Trace.TraceError($"ProcessSubscriptions failed {ex.Message}");
            return false;
        }
    }

    private static async Task SendExceptionMessageToOwner(string jobName, Exception ex)
    {
        if (long.TryParse(Environment.GetEnvironmentVariable("OwnerTelegramgId"), out var ownerId))
        {
            var telegramService = Bootstrapper.Resolve<ITgBotService>();
            await telegramService.SendMessageAsync(ownerId, $"{jobName} failed", CancellationToken.None);
            await telegramService.SendMessageAsync(ownerId, $"{ex.Message}", CancellationToken.None);
            Trace.TraceError(ex.StackTrace);
        }
    }
}