using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Common;
using theatrel.Common.Enums;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Subscriptions;
using theatrel.Lib;
using theatrel.TLBot.Interfaces;

namespace theatrel.Subscriptions
{
    public class SubscriptionProcessor : ISubscriptionProcessor
    {
        private readonly ITgBotService _telegramService;
        private readonly IDbService _dbService;
        private readonly IFilterService _filterChecker;

        public SubscriptionProcessor(ITgBotService telegramService, IFilterService filterChecker, IDbService dbService)
        {
            _telegramService = telegramService;
            _dbService = dbService;
            _filterChecker = filterChecker;
        }

        public async Task<bool> ProcessSubscriptions()
        {
            Trace.TraceInformation("Process subscriptions started");

            using ISubscriptionsRepository subscriptionRepository = _dbService.GetSubscriptionRepository();
            PlaybillChangeEntity[] changes = subscriptionRepository.GetAllChanges();

            Dictionary<long, Dictionary<int, PlaybillChangeEntity>> messagesDictionary = new Dictionary<long, Dictionary<int, PlaybillChangeEntity>>();

            var subscriptions = subscriptionRepository.GetAllWithFilter().ToArray();
            foreach (SubscriptionEntity subscription in subscriptions)
            {
                var filter = subscription.PerformanceFilter;

                Trace.TraceInformation($"Process filter:{filter.Id} performanceId: {filter.PlaybillId} user:{subscription.TelegramUserId} {filter.StartDate:g} {filter.EndDate:g}");

                PlaybillChangeEntity[] performanceChanges = filter.PlaybillId > 0
                    ? changes
                        .Where(p => p.PlaybillEntity.PerformanceId == filter.PlaybillId
                                    && p.LastUpdate > subscription.LastUpdate
                                    && (subscription.TrackingChanges & p.ReasonOfChanges) != 0)
                        .OrderBy(p => p.LastUpdate).ToArray()
                    : changes
                        .Where(p => _filterChecker.IsDataSuitable(p.PlaybillEntity.Performance.Location.Name,
                                        p.PlaybillEntity.Performance.Type.TypeName, p.PlaybillEntity.When, filter)
                                    && p.LastUpdate > subscription.LastUpdate
                                    && (subscription.TrackingChanges & p.ReasonOfChanges) != 0)
                        .OrderBy(p => p.LastUpdate).ToArray();

                Trace.TraceInformation($"Found {performanceChanges.Length} changes.");

                if (!performanceChanges.Any())
                    continue;

                if (!messagesDictionary.ContainsKey(subscription.TelegramUserId))
                    messagesDictionary.Add(subscription.TelegramUserId, new Dictionary<int, PlaybillChangeEntity>());

                Dictionary<int, PlaybillChangeEntity> changesDictionary = messagesDictionary[subscription.TelegramUserId];
                foreach (var change in performanceChanges)
                {
                    if (!changesDictionary.ContainsKey(change.PlaybillEntityId))
                    {
                        changesDictionary[change.PlaybillEntityId] = change;
                        continue;
                    }

                    if (changesDictionary[change.PlaybillEntityId].LastUpdate < change.LastUpdate)
                        changesDictionary[change.PlaybillEntityId] = change;
                }
            }

            foreach (var userData in messagesDictionary)
            {
                if (!await SendMessages(userData.Key, userData.Value.Select(d => d.Value).ToArray()))
                    continue;

                //if message was sent we should update LastUpdate for users subscriptions
                foreach (var subscription in subscriptions.Where(s => s.TelegramUserId == userData.Key))
                {
                    subscription.LastUpdate = userData.Value.Last().Value.LastUpdate;
                    await subscriptionRepository.Update(subscription);
                }
            }

            Trace.TraceInformation("Process subscription finished");
            return true;
        }

        private async Task<bool> SendMessages(long tgUserId, PlaybillChangeEntity[] changes)
        {
            string message = string.Join(Environment.NewLine, changes.Select(GetChangeDescription));

            return await _telegramService.SendEscapedMessageAsync(tgUserId, message, CancellationToken.None);
        }

        private string GetChangeDescription(PlaybillChangeEntity change)
        {
            var cultureRu = CultureInfo.CreateSpecificCulture("ru");
            string formattedDate = change.PlaybillEntity.When.AddHours(3).ToString("ddMMM HH:mm", cultureRu);

            var playbillEntity = change.PlaybillEntity;
            string performanceString = $"{formattedDate} {playbillEntity.Performance.Name}".EscapeMessageForMarkupV2();

            string fullInfo = string.IsNullOrWhiteSpace(playbillEntity.Url) || playbillEntity.Url.Equals(CommonTags.NotDefined, StringComparison.OrdinalIgnoreCase)
                ? performanceString
                : $"[{performanceString}]({playbillEntity.Url.EscapeMessageForMarkupV2()})";

            ReasonOfChanges reason = (ReasonOfChanges)change.ReasonOfChanges;
            switch (reason)
            {
                case ReasonOfChanges.Creation:
                    string month = cultureRu.DateTimeFormat.GetMonthName(change.PlaybillEntity.When.Month);
                    return $"Новое в афише на {month}: {fullInfo}";
                case ReasonOfChanges.PriceDecreased:
                    return $"Снижена цена {fullInfo} цена билета от {change.MinPrice}";
                case ReasonOfChanges.PriceIncreased:
                    return $"Билеты подорожали {fullInfo} цена билета от {change.MinPrice}";
                case ReasonOfChanges.StartSales:
                    return $"Появились в продаже билеты {fullInfo} цена билета от {change.MinPrice}";
            }

            return null;
        }
    }
}
