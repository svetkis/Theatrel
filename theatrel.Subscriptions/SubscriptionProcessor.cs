using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
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
            PlaybillChangeEntity[] changes = _dbContext.PerformanceChanges
                .Include(c => c.PlaybillEntity)
                    .ThenInclude(p => p.Performance)
                    .ThenInclude(p => p.Type)
                .Include(c => c.PlaybillEntity)
                    .ThenInclude(p => p.Performance)
                    .ThenInclude(p => p.Location)
                .ToArray();

            bool dbIsDirty = false;
            foreach (SubscriptionEntity subscription in _dbContext.Subscriptions)
            {
                var filter = subscription.PerformanceFilter;

                PlaybillChangeEntity[] performanceChanges = filter.PerformanceId >= 0
                    ? changes
                        .Where(p => p.PlaybillEntity.PerformanceId == filter.PerformanceId
                                    && p.LastUpdate > subscription.LastUpdate
                                    && (subscription.TrackingChanges & p.ReasonOfChanges) != 0)
                        .OrderBy(p => p.LastUpdate).ToArray()
                    : changes
                        .Where(p => _filterChecker.IsDataSuitable(p.PlaybillEntity.Performance.Location.Name,
                                        p.PlaybillEntity.Performance.Type.TypeName, p.PlaybillEntity.When, subscription.PerformanceFilter)
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

        private async Task<bool> SendMessages(SubscriptionEntity subscription, PlaybillChangeEntity[] changes)
        {
            string message = string.Join(Environment.NewLine,
                changes.Select(change => GetChangeDescription(subscription, change)));

            return await _telegramService.SendMessageAsync(subscription.Id, message);
        }

        private string GetChangeDescription(SubscriptionEntity subscription, PlaybillChangeEntity change)
        {
            var culture = CultureInfo.CreateSpecificCulture(subscription.TelegramUser.Culture);

            ReasonOfChanges reason = (ReasonOfChanges) change.ReasonOfChanges;
            switch (reason)
            {
                case ReasonOfChanges.Creation:
                    string month = culture.DateTimeFormat.GetMonthName(change.PlaybillEntity.When.Month);
                    return $"Новое в афише на {month}: {change.PlaybillEntity.Performance.Name} {change.PlaybillEntity.Url}";
                case ReasonOfChanges.PriceDecreased:
                    return $"Снижена цена на {change.PlaybillEntity.Performance.Name} цена билета от {change.MinPrice} {change.PlaybillEntity.Url}";
                case ReasonOfChanges.PriceIncreased:
                    return $"Билеты подорожали {change.PlaybillEntity.Performance.Name} цена билета от {change.MinPrice} {change.PlaybillEntity.Url}";
                case ReasonOfChanges.StartSales:
                    return $"Появились в продаже билеты на {change.PlaybillEntity.Performance.Name} цена билета от {change.MinPrice} {change.PlaybillEntity.Url}";
            }

            return null;
        }
    }
}
