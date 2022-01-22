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
            foreach (var filter in AddFiltersForNearestMonths(filters, 3))
            {
                await using (var scope = Bootstrapper.RootScope.BeginLifetimeScope())
                {
                    IDbPlaybillUpdater updater = scope.Resolve<IDbPlaybillUpdater>();

                    Trace.TraceInformation($"Update playbill Mariinski for interval {filter.StartDate.ToString("d", culture)} {filter.EndDate.ToString("d", culture)}");
                    await updater.UpdateAsync(1, filter.StartDate, filter.EndDate, cToken);
                }

                MemoryHelper.Collect(true);
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
            foreach (var filter in AddFiltersForNearestMonths(filters, 3))
            {
                await using (var scope = Bootstrapper.RootScope.BeginLifetimeScope())
                {
                    IDbPlaybillUpdater updater = scope.Resolve<IDbPlaybillUpdater>();

                    Trace.TraceInformation($"Update Michailovsky playbill for interval {filter.StartDate.ToString("d", culture)} {filter.EndDate.ToString("d", culture)}");
                    await updater.UpdateAsync(2, filter.StartDate, filter.EndDate, cToken);

                }

                MemoryHelper.Collect(true);
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

    private static IPerformanceFilter[] AddFiltersForNearestMonths(IEnumerable<IPerformanceFilter> filters, int monthsCount)
    {
        IFilterService filterService = Bootstrapper.Resolve<IFilterService>();

        List<int> addedMonths = new List<int>();
        var performanceFilters = filters as IPerformanceFilter[] ?? filters.ToArray();
        foreach (var month in Enumerable.Range(0, monthsCount).Select(n => DateTime.UtcNow.Month + n))
        {
            int m = NormalizeMonth(month);
            int y = month > 12 ? DateTime.UtcNow.Year + 1 : DateTime.UtcNow.Year;
            var date = new DateTime(y, m, 1, 0, 0,0, DateTimeKind.Utc);
            if (performanceFilters.All(f => f.StartDate != date))
                addedMonths.Add(month);
        }

        if (!addedMonths.Any())
            return performanceFilters.ToArray();

        List<IPerformanceFilter> newFilters = new List<IPerformanceFilter>(performanceFilters);

        foreach (var month in addedMonths)
        {
            int m = NormalizeMonth(month);

            int y = month > 12 ? DateTime.UtcNow.Year + 1 : DateTime.UtcNow.Year;
            var date = new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);

            newFilters.Add(filterService.GetFilter(date, date.AddMonths(1)));
        }

        return newFilters.ToArray();
    }

    private static int NormalizeMonth(int month)
    {
        int m = month % 12;
        return m == 0 ? 12 : m;
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