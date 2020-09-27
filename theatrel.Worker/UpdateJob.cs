using Autofac;
using Quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Common;
using theatrel.Interfaces.DataUpdater;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Subscriptions;
using theatrel.TLBot.Interfaces;

namespace theatrel.Worker
{
    public class UpdateJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            Trace.TraceInformation("UpdateJob was started");

            MemoryHelper.LogMemoryUsage();
            if (!await UpdatePlaybill(context.CancellationToken))
                return;

            GC.Collect();
            MemoryHelper.LogMemoryUsage();

            if (!await ProcessSubscriptions(context.CancellationToken))
                return;

            GC.Collect();
            MemoryHelper.LogMemoryUsage();

            if (!await PlaybillCleanup(context.CancellationToken))
                return;

            GC.Collect();
            MemoryHelper.LogMemoryUsage();

            if (!await SubscriptionsCleanup(context.CancellationToken))
                return;

            GC.Collect();
            MemoryHelper.LogMemoryUsage();

            Trace.TraceInformation("UpdateJob was finished");
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
                    await using (var scope = Bootstrapper.BeginLifetimeScope())
                    {
                        IDbPlaybillUpdater updater = scope.Resolve<IDbPlaybillUpdater>();

                        Trace.TraceInformation($"Update playbill for interval {filter.StartDate.ToString("d", culture)} {filter.EndDate.ToString("d", culture)}");
                        await updater.UpdateAsync(1, filter.StartDate, filter.EndDate, cToken);
                    }

                    //we need to be care with memory because heroku has memory limit for free app
                    GC.Collect();
                    MemoryHelper.LogMemoryUsage();
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

        private IPerformanceFilter[] AddFiltersForNearestMonths(IEnumerable<IPerformanceFilter> filters, int monthsCount)
        {
            IFilterService filterService = Bootstrapper.Resolve<IFilterService>();

            List<int> addedMonths = new List<int>();
            var performanceFilters = filters as IPerformanceFilter[] ?? filters.ToArray();
            foreach (var month in Enumerable.Range(0, monthsCount).Select(n => DateTime.Now.Month + n))
            {
                int m = NormalizeMonth(month);
                int y = month > 12 ? DateTime.Now.Year + 1 : DateTime.Now.Year;
                var date = new DateTime(y, m, 1);
                if (performanceFilters.All(f => f.StartDate != date))
                    addedMonths.Add(month);
            }

            if (!addedMonths.Any())
                return performanceFilters.ToArray();

            List<IPerformanceFilter> newFilters = new List<IPerformanceFilter>(performanceFilters);

            foreach (var month in addedMonths)
            {
                int m = NormalizeMonth(month);

                int y = month > 12 ? DateTime.Now.Year + 1 : DateTime.Now.Year;
                var date = new DateTime(y, m, 1);

                newFilters.Add(filterService.GetFilter(date, date.AddMonths(1)));
            }

            return newFilters.ToArray();
        }

        private int NormalizeMonth(int month)
        {
            int m = month % 12;
            return m == 0 ? 12 : m;
        }

        public async Task<bool> PlaybillCleanup(CancellationToken cToken)
        {
            try
            {
                Trace.TraceInformation("PlaybillCleanup CleanUp");

                await using var scope = Bootstrapper.BeginLifetimeScope();
                using IPlaybillCleanUpService cleanUpService = scope.Resolve<IPlaybillCleanUpService>();
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
                Trace.TraceInformation("Subscriptions CleanUp");

                await using var scope = Bootstrapper.BeginLifetimeScope();
                ISubscriptionsCleanupService cleanUpService = scope.Resolve<ISubscriptionsCleanupService>();
                await cleanUpService.CleanUp();

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

        private async Task SendExceptionMessageToOwner(string jobName, Exception ex)
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
}
