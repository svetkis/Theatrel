using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using theatrel.Common.Enums;
using theatrel.DataAccess;
using theatrel.DataAccess.Entities;
using theatrel.Interfaces;
using theatrel.TLBot.Interfaces;

namespace theatrel.Subscriptions
{
    public class SubscriptionProcessor : ISubscriptionProcessor
    {
        private readonly ITLBotService _telegramService;
        private readonly AppDbContext _dbContext;
        private readonly IFilterChecker _filterChecker;

        public SubscriptionProcessor(ITLBotService telegramService, IFilterChecker filterChecker, AppDbContext dbContext)
        {
            _telegramService = telegramService;
            _dbContext = dbContext;
            _filterChecker = filterChecker;
        }

        public async Task<bool> ProcessSubscriptions()
        {
            Trace.TraceInformation("Process subscriptions started");
            LocalView<PerformanceChangeEntity> changes = _dbContext.PerformanceChanges.Local;

            bool dbIsDirty = false;
            foreach (SubscriptionEntity subscription in _dbContext.Subscriptions)
            {
                var filter = subscription.PerformanceFilter;

                PerformanceChangeEntity[] performanceChanges = filter.PerformanceId >= 0
                    ? changes
                        .Where(p => p.PerformanceEntityId == filter.PerformanceId
                                    && p.LastUpdate > subscription.LastUpdate
                                    && (subscription.TrackingChanges & p.ReasonOfChanges) != 0)
                        .OrderBy(p => p.LastUpdate).ToArray()
                    : changes
                        .Where(p => _filterChecker.IsDataSuitable(p.PerformanceEntity, subscription.PerformanceFilter)
                                    && p.LastUpdate > subscription.LastUpdate
                                    && (subscription.TrackingChanges & p.ReasonOfChanges) != 0)
                        .OrderBy(p => p.LastUpdate).ToArray();

                if (!performanceChanges.Any() || !await SendMessages(subscription, performanceChanges))
                    continue;

                subscription.LastUpdate = performanceChanges.Last().LastUpdate;
                dbIsDirty = true;
            }

            if (dbIsDirty)
                await _dbContext.SaveChangesAsync();

            return true;
        }

        private async Task<bool> SendMessages(SubscriptionEntity subscription, PerformanceChangeEntity[] changes)
        {
            string message = string.Join(Environment.NewLine,
                changes.Select(change => GetChangeDescription(subscription, change)));

            return await _telegramService.SendMessageAsync(subscription.Id, message);
        }

        private string GetChangeDescription(SubscriptionEntity subscription, PerformanceChangeEntity change)
        {
            var culture = CultureInfo.CreateSpecificCulture(subscription.TelegramUser.Culture);

            ReasonOfChanges reason = (ReasonOfChanges) change.ReasonOfChanges;
            switch (reason)
            {
                case ReasonOfChanges.Creation:
                    string month = culture.DateTimeFormat.GetMonthName(change.PerformanceEntity.DateTime.Month);
                    return $"Новое в афише на {month}: {change.PerformanceEntity.Name} {change.PerformanceEntity.Url}";
                case ReasonOfChanges.PriceDecreased:
                    return $"Снижена цена на {change.PerformanceEntity.Name} цена билета от {change.MinPrice} {change.PerformanceEntity.Url}";
                case ReasonOfChanges.PriceIncreased:
                    return $"Билеты подорожали {change.PerformanceEntity.Name} цена билета от {change.MinPrice} {change.PerformanceEntity.Url}";
                case ReasonOfChanges.StartSales:
                    return $"Появились в продаже билеты на {change.PerformanceEntity.Name} цена билета от {change.MinPrice} {change.PerformanceEntity.Url}";
            }

            return null;
        }
    }
}
