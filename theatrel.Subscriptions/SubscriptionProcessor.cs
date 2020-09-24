using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
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
            var subscriptions = subscriptionRepository.GetAllWithFilter().ToArray();

            DateTime lastSubscriptionsUpdate = subscriptions.Min(s => s.LastUpdate);

            PlaybillChangeEntity[] changes = subscriptionRepository.GetFreshChanges(lastSubscriptionsUpdate);

            Dictionary<long, Dictionary<int, PlaybillChangeEntity>> messagesDictionary = new Dictionary<long, Dictionary<int, PlaybillChangeEntity>>();

            foreach (SubscriptionEntity subscription in subscriptions)
            {
                var filter = subscription.PerformanceFilter;

                Trace.TraceInformation($"Filter:{filter.Id} PlaybillId: {filter.PlaybillId} user:{subscription.TelegramUserId} {filter.StartDate:yy-MM-dd} {filter.EndDate:yy-MM-dd}");

                PlaybillChangeEntity[] performanceChanges;

                if (!string.IsNullOrEmpty(filter.PerformanceName))
                {
                    performanceChanges = changes.Where(p =>
                        string.Equals(p.PlaybillEntity.Performance.Name, filter.PerformanceName, StringComparison.OrdinalIgnoreCase)
                        && _filterChecker.IsDataSuitable(p.PlaybillEntity.Performance.Name, p.PlaybillEntity.Performance.Location.Name,
                            p.PlaybillEntity.Performance.Type.TypeName, p.PlaybillEntity.When, filter)
                        && p.LastUpdate > subscription.LastUpdate
                        && (subscription.TrackingChanges & p.ReasonOfChanges) != 0)
                        .OrderBy(p => p.LastUpdate).ToArray();
                }
                else if (filter.PlaybillId > 0)
                {
                    performanceChanges = changes
                        .Where(p => p.PlaybillEntity.PerformanceId == filter.PlaybillId
                                    && p.LastUpdate > subscription.LastUpdate
                                    && (subscription.TrackingChanges & p.ReasonOfChanges) != 0)
                        .OrderBy(p => p.LastUpdate).ToArray();
                }
                else
                {
                    performanceChanges = changes
                        .Where(p => _filterChecker.IsDataSuitable(p.PlaybillEntity.Performance.Name, p.PlaybillEntity.Performance.Location.Name,
                                        p.PlaybillEntity.Performance.Type.TypeName, p.PlaybillEntity.When, filter)
                                    && p.LastUpdate > subscription.LastUpdate
                                    && (subscription.TrackingChanges & p.ReasonOfChanges) != 0)
                        .OrderBy(p => p.LastUpdate).ToArray();
                }

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

        private string GetChangesDescription(PlaybillChangeEntity[] changes)
        {
            StringBuilder sb = new StringBuilder();

            var cultureRu = CultureInfo.CreateSpecificCulture("ru");

            switch ((ReasonOfChanges)changes.First().ReasonOfChanges)
            {
                case ReasonOfChanges.Creation:
                    sb.AppendLine("Новое в афише:");
                    break;
                case ReasonOfChanges.PriceDecreased:
                    sb.AppendLine("Снижена цена:");
                    break;
                case ReasonOfChanges.PriceIncreased:
                    sb.AppendLine("Билеты подорожали:");
                    break;
                case ReasonOfChanges.StartSales:
                    sb.AppendLine("Появились в продаже билеты:");
                    break;
            }

            foreach (var change in changes)
            {
                PlaybillEntity playbillEntity = change.PlaybillEntity;
                string formattedDate = playbillEntity.When.AddHours(3).ToString("ddMMM HH:mm", cultureRu);


                string firstPart = $"{formattedDate} {playbillEntity.Performance.Location.Name} {playbillEntity.Performance.Type.TypeName}"
                    .EscapeMessageForMarkupV2();

                string escapedName = $"\"{playbillEntity.Performance.Name}\"".EscapeMessageForMarkupV2();
                string performanceString = string.IsNullOrWhiteSpace(playbillEntity.Url) || playbillEntity.Url == CommonTags.NotDefined
                    ? escapedName
                    : $"[{escapedName}]({playbillEntity.Url.EscapeMessageForMarkupV2()})";

                string lastPart = string.IsNullOrWhiteSpace(playbillEntity.TicketsUrl) || playbillEntity.TicketsUrl == CommonTags.NotDefined
                    ? $"от {change.MinPrice}"
                    : $"от [{change.MinPrice}]({playbillEntity.TicketsUrl.EscapeMessageForMarkupV2()})";

                sb.AppendLine($"{firstPart} {performanceString} {lastPart}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private async Task<bool> SendMessages(long tgUserId, PlaybillChangeEntity[] changes)
        {
            var groups = changes.GroupBy(change => change.ReasonOfChanges).Select(group => group.ToArray());
            string message = string.Join(Environment.NewLine, groups.Select(GetChangesDescription));
            return await _telegramService.SendEscapedMessageAsync(tgUserId, message, CancellationToken.None);
        }
    }
}
