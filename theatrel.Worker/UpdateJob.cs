using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Quartz;
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

                if (filters == null)
                    return false;

                foreach (var filter in filters)
                {
                    await using var scope = Bootstrapper.BeginLifetimeScope();
                    {
                        IDbPlaybillUpdater updater = scope.Resolve<IDbPlaybillUpdater>();

                        Trace.TraceInformation($"Update playbill for interval {filter.StartDate:g} {filter.EndDate:g}");
                        await updater.UpdateAsync(1, filter.StartDate, filter.EndDate, cToken);
                    }

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

        public async Task<bool> SubscriptionsCleanup(CancellationToken cToken)
        {
            try
            {
                Trace.TraceInformation("Subscriptions CleanUp");

                await using var scope = Bootstrapper.BeginLifetimeScope();
                using IPlaybillCleanUpService cleanUpService = scope.Resolve<IPlaybillCleanUpService>();

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
                await telegramService.SendMessageAsync(ownerId, $"{jobName} failed");
                await telegramService.SendMessageAsync(ownerId, $"{ex.Message}");
                Trace.TraceError(ex.StackTrace);
            }
        }
    }
}
