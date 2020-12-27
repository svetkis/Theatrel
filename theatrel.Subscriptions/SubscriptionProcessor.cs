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
using theatrel.Common.FormatHelper;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Subscriptions;
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

            Dictionary<long, Dictionary<int, List<PlaybillChangeEntity>>> messagesDictionary = new Dictionary<long, Dictionary<int, List<PlaybillChangeEntity>>>();

            foreach (SubscriptionEntity subscription in subscriptions)
            {
                var filter = subscription.PerformanceFilter;

                PlaybillChangeEntity[] performanceChanges;

                if (!string.IsNullOrEmpty(filter.PerformanceName))
                {
                    performanceChanges = changes.Where(p =>
                        string.Equals(p.PlaybillEntity.Performance.Name, filter.PerformanceName, StringComparison.OrdinalIgnoreCase)
                        && _filterChecker.IsDataSuitable(p.PlaybillEntity.Id, p.PlaybillEntity.Performance.Name, p.PlaybillEntity.Performance.Location.Name,
                            p.PlaybillEntity.Performance.Type.TypeName, p.PlaybillEntity.When, filter)
                        && p.LastUpdate > subscription.LastUpdate
                        && (subscription.TrackingChanges & p.ReasonOfChanges) != 0
                        && subscription.TrackingChanges != 0)
                        .OrderBy(p => p.LastUpdate).ToArray();
                }
                else if (filter.PlaybillId > 0)
                {
                    performanceChanges = changes
                        .Where(p => p.PlaybillEntity.Id == filter.PlaybillId
                                    && p.LastUpdate > subscription.LastUpdate
                                    && (subscription.TrackingChanges & p.ReasonOfChanges) != 0
                                    && subscription.TrackingChanges != 0)
                        .OrderBy(p => p.LastUpdate).ToArray();
                }
                else
                {
                    performanceChanges = changes
                        .Where(p => _filterChecker.IsDataSuitable(p.PlaybillEntityId, p.PlaybillEntity.Performance.Name, p.PlaybillEntity.Performance.Location.Name,
                                        p.PlaybillEntity.Performance.Type.TypeName, p.PlaybillEntity.When, filter)
                                    && p.LastUpdate > subscription.LastUpdate
                                    && (subscription.TrackingChanges & p.ReasonOfChanges) != 0
                                    && subscription.TrackingChanges != 0)
                        .OrderBy(p => p.LastUpdate).ToArray();
                }

                if (!performanceChanges.Any())
                    continue;

                if (!messagesDictionary.ContainsKey(subscription.TelegramUserId))
                    messagesDictionary.Add(subscription.TelegramUserId, new Dictionary<int, List<PlaybillChangeEntity>>());

                Dictionary<int, List<PlaybillChangeEntity>> changesDictionary = messagesDictionary[subscription.TelegramUserId];
                foreach (var change in performanceChanges)
                {
                    if (!changesDictionary.ContainsKey(change.PlaybillEntityId))
                    {
                        changesDictionary[change.PlaybillEntityId] = new List<PlaybillChangeEntity> {change};
                        continue;
                    }

                    changesDictionary[change.PlaybillEntityId].Add(change);
                }
            }

            foreach (var userData in messagesDictionary)
            {
                var changesToProcess = userData.Value.SelectMany(d => d.Value.ToArray()).Distinct().ToArray();
                if (!await SendMessages(userData.Key, changesToProcess))
                    continue;

                //if message was sent we should update LastUpdate for users subscriptions
                foreach (var subscription in subscriptions.Where(s => s.TelegramUserId == userData.Key))
                {
                    subscription.LastUpdate = DateTime.Now;
                    await subscriptionRepository.Update(subscription);
                }
            }

            Trace.TraceInformation("Process subscription finished");
            return true;
        }

        private readonly ReasonOfChanges[] _reasonToShowCast = {ReasonOfChanges.CastWasChanged, ReasonOfChanges.CastWasSet, ReasonOfChanges.Creation, ReasonOfChanges.StartSales};
        private string GetChangesDescription(PlaybillChangeEntity[] changes)
        {
            StringBuilder sb = new StringBuilder();

            var cultureRu = CultureInfo.CreateSpecificCulture("ru");

            ReasonOfChanges reason = (ReasonOfChanges) changes.First().ReasonOfChanges;

            switch (reason)
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
                case ReasonOfChanges.StopSales:
                    sb.AppendLine("Стоп продаж:");
                    break;
                case ReasonOfChanges.WasMoved:
                    sb.AppendLine("Перенесены на другую дату:");
                    break;
                case ReasonOfChanges.StopSale:
                    sb.AppendLine("Закончились билеты:");
                    break;
                case ReasonOfChanges.CastWasSet:
                    sb.AppendLine("Объявлен состав исполнителей:");
                    break;
                case ReasonOfChanges.CastWasChanged:
                    sb.AppendLine("Состав исполнителей изменен:");
                    break;
            }

            foreach (var change in changes)
            {
                PlaybillEntity playbillEntity = change.PlaybillEntity;
                string formattedDate = playbillEntity.When.AddHours(3).ToString("ddMMM HH:mm", cultureRu);


                string firstPart = $"{formattedDate} {playbillEntity.Performance.Location.Name} {playbillEntity.Performance.Type.TypeName}"
                    .EscapeMessageForMarkupV2();

                string description = !string.IsNullOrEmpty(playbillEntity.Description)
                    ? $" ({playbillEntity.Description})".EscapeMessageForMarkupV2()
                    : string.Empty;

                string escapedName = $"\"{playbillEntity.Performance.Name}\"".EscapeMessageForMarkupV2();
                string performanceString = string.IsNullOrWhiteSpace(playbillEntity.Url) || CommonTags.TechnicalStateTags.Contains(playbillEntity.Url)
                    ? escapedName
                    : $"[{escapedName}]({playbillEntity.Url.EscapeMessageForMarkupV2()})";

                bool noTicketsUrl = string.IsNullOrWhiteSpace(playbillEntity.TicketsUrl) ||
                                    CommonTags.TechnicalStateTags.Contains(playbillEntity.TicketsUrl);

                if (noTicketsUrl && change.MinPrice > 0)
                    Trace.TraceInformation($"noTicketsUrl {playbillEntity.TicketsUrl} {playbillEntity.Performance.Name} {playbillEntity.When} {change.MinPrice}");

                string lastPart = change.MinPrice == 0 || noTicketsUrl
                    ? string.Empty
                    : $"от [{change.MinPrice}]({playbillEntity.TicketsUrl.EscapeMessageForMarkupV2()})";

                sb.AppendLine($"{firstPart} {performanceString}{description} {lastPart}");
                if (playbillEntity.Cast != null && _reasonToShowCast.Contains(reason))
                {
                    IDictionary<string, IList<ActorEntity>> actorsDictionary = new Dictionary<string, IList<ActorEntity>>();
                    foreach (var item in playbillEntity.Cast)
                    {
                        if (!actorsDictionary.ContainsKey(item.Role.CharacterName))
                            actorsDictionary[item.Role.CharacterName] = new List<ActorEntity>();

                        actorsDictionary[item.Role.CharacterName].Add(item.Actor);
                    }

                    foreach (var group in actorsDictionary.OrderBy(kp => kp.Key, CharactersComparer.Create()))
                    {
                        string actorsList = string.Join(", ", group.Value.Select(item =>
                            item.Url == CommonTags.NotDefinedTag || string.IsNullOrEmpty(item.Url)
                                ? item.Name.EscapeMessageForMarkupV2()
                                : $"[{item.Name.EscapeMessageForMarkupV2()}]({item.Url.EscapeMessageForMarkupV2()})"));

                        bool isPhonogram = group.Key == CommonTags.Conductor && group.Value.First().Name == CommonTags.Phonogram;

                        string characterPart = group.Key == CommonTags.Actor || isPhonogram
                            ? string.Empty
                            : $"{group.Key} - ".EscapeMessageForMarkupV2();

                        sb.AppendLine($"{characterPart}{actorsList}");
                    }
                }

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
