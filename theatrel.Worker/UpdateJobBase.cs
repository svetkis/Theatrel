﻿using Autofac;
using Quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.DataUpdater;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Subscriptions;
using theatrel.TLBot.Interfaces;

namespace theatrel.Worker;

[DisallowConcurrentExecution]
public class UpdateMariinskyJob : UpdateJobBase
{
    protected override int TheatreId => 1;
}

[DisallowConcurrentExecution]
public class UpdateMichailovskyJob : UpdateJobBase
{
    protected override int TheatreId => 2;
}

public class ProlongSubscriptionJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        if (!await ProlongSubscriptions(context.CancellationToken))
            return;
    }

    private async Task<bool> ProlongSubscriptions(CancellationToken cToken)
    {
        try
        {
            await using var scope = Bootstrapper.BeginLifetimeScope();
            ISubscriptionsUpdaterService subscriptionsUpdaterService = scope.Resolve<ISubscriptionsUpdaterService>();

            await subscriptionsUpdaterService.ProlongSubscriptions(cToken);

            await subscriptionsUpdaterService.ProlongSubscriptionsVk(cToken);

            return true;
        }
        catch (Exception ex)
        {
            await SendExceptionMessageToOwner("ProlongSubscriptions", ex);
            Trace.TraceError($"ProlongSubscriptions failed {ex.Message}");
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

public abstract class UpdateJobBase : IJob
{
    protected abstract int TheatreId { get; }

    public async Task Execute(IJobExecutionContext context)
    {
        if (!await ProlongSubscriptions(context.CancellationToken))
            return;

        if (!await UpdatePlaybill(context.CancellationToken))
            return;

        if (!await ProcessSubscriptions(context.CancellationToken))
            return;

        if (!await PlaybillCleanup(context.CancellationToken))
            return;

        if (!await SubscriptionsCleanup(context.CancellationToken))
            return;
    }

    public async Task<bool> UpdatePlaybill(CancellationToken cToken)
    {
        try
        {
            ISubscriptionService subscriptionServices = Bootstrapper.Resolve<ISubscriptionService>();
            var filters = subscriptionServices.GetUpdateFilters();

            var culture = CultureInfo.CreateSpecificCulture("ru");
            foreach (var filter in AddFiltersForNearestMonths(filters, 6))
            {
                await using var scope = Bootstrapper.BeginLifetimeScope();

                IDbPlaybillUpdater updater = scope.Resolve<IDbPlaybillUpdater>();

                await updater.Update(TheatreId, filter.StartDate, filter.EndDate, cToken);
            }

            return true;
        }
        catch (Exception ex)
        {
            await SendExceptionMessageToOwner("UpdatePlaybill", ex);
            Trace.TraceError($"UpdatePlaybill failed {ex.Message}");
            return false;
        }
    }

    private static IPerformanceFilter[] AddFiltersForNearestMonths(IEnumerable<IPerformanceFilter> filters, int monthsCount)
    {
        var validDateRangeFilters = filters.Where(x => x.StartDate > DateTime.MinValue).ToArray();

        var additionalRanges = Enumerable
            .Range(0, monthsCount)
            .Select(n => GetDate(DateTime.UtcNow.Month + n))
            .Where(d => !validDateRangeFilters.Any(f => f.StartDate == d))
            .ToArray();

        if (!additionalRanges.Any())
            return validDateRangeFilters;

        IFilterService filterService = Bootstrapper.Resolve<IFilterService>();
        var additonalFilters = additionalRanges.Select(d => filterService.GetOneMonthFilter(d));

        return validDateRangeFilters.Concat(additonalFilters).ToArray();
    }

    private static DateTime GetDate(int addedMonth)
    {
        int month = addedMonth > 12 ? addedMonth % 12 : addedMonth;
        int year = addedMonth > 12 ? DateTime.UtcNow.Year + 1 : DateTime.UtcNow.Year;

        return new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public async Task<bool> PlaybillCleanup(CancellationToken cToken)
    {
        try
        {
            await using var scope = Bootstrapper.BeginLifetimeScope();
            IPlaybillCleanUpService cleanUpService = scope.Resolve<IPlaybillCleanUpService>();
            await cleanUpService.CleanUp();

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
            await using var scope = Bootstrapper.BeginLifetimeScope();
            ISubscriptionsUpdaterService cleanUpService = scope.Resolve<ISubscriptionsUpdaterService>();
            await cleanUpService.CleanUpOutDatedSubscriptions();

            return true;
        }
        catch (Exception ex)
        {
            await SendExceptionMessageToOwner("SubscriptionsCleanup", ex);
            Trace.TraceError($"SubscriptionsCleanup failed {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ProlongSubscriptions(CancellationToken cToken)
    {
        try
        {
            await using var scope = Bootstrapper.BeginLifetimeScope();
            ISubscriptionsUpdaterService subscriptionsUpdaterService = scope.Resolve<ISubscriptionsUpdaterService>();

            await subscriptionsUpdaterService.ProlongSubscriptions(cToken);

            await subscriptionsUpdaterService.ProlongSubscriptionsVk(cToken);

            return true;
        }
        catch (Exception ex)
        {
            await SendExceptionMessageToOwner("ProlongSubscriptions failed", ex);
            Trace.TraceError($"ProlongSubscriptions failed {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ProcessSubscriptions(CancellationToken cToken)
    {
        try
        {
            await using var scope = Bootstrapper.BeginLifetimeScope();

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