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
using theatrel.Interfaces.TimeZoneService;
using theatrel.TLBot.Interfaces;

namespace theatrel.Subscriptions;

public class SubscriptionProcessor : ISubscriptionProcessor
{
    private readonly ITgBotService _telegramService;
    private readonly IDbService _dbService;
    private readonly IFilterService _filterChecker;
    private readonly ITimeZoneService _timeZoneService;

    public SubscriptionProcessor(ITgBotService telegramService, IFilterService filterChecker, ITimeZoneService timeZoneService, IDbService dbService)
    {
        _telegramService = telegramService;
        _dbService = dbService;
        _filterChecker = filterChecker;
        _timeZoneService = timeZoneService;
    }

    public async Task<bool> ProcessSubscriptions()
    {
        Trace.TraceInformation("Process subscriptions started");

        using ISubscriptionsRepository subscriptionRepository = _dbService.GetSubscriptionRepository();
        var subscriptions = subscriptionRepository.GetAllWithFilter().ToArray();

        using IPlaybillRepository playbillRepository =
            subscriptions.Any(s => !string.IsNullOrEmpty(s.PerformanceFilter.Actor))
            ? _dbService.GetPlaybillRepository()
            : null;

        if (!subscriptions.Any())
            return true;

        DateTime lastSubscriptionsUpdate = subscriptions.Min(s => s.LastUpdate).ToUniversalTime();

        PlaybillChangeEntity[] changes = subscriptionRepository.GetFreshChanges(lastSubscriptionsUpdate);

        Dictionary<long, Dictionary<int, List<PlaybillChangeEntity>>> messagesDictionary = new Dictionary<long, Dictionary<int, List<PlaybillChangeEntity>>>();

        foreach (SubscriptionEntity subscription in subscriptions)
        {
            if (subscription.TrackingChanges == 0)
                continue;

            PerformanceFilterEntity filter = subscription.PerformanceFilter;

            var changesToSubscription = changes.Where(x =>
                x.LastUpdate > subscription.LastUpdate && (subscription.TrackingChanges & x.ReasonOfChanges) != 0)
                .OrderBy(p => p.LastUpdate);

            PlaybillChangeEntity[] performanceChanges;

            if (!string.IsNullOrEmpty(filter.PerformanceName))
            {
                performanceChanges = changesToSubscription
                    .Where(change => FilterByPerformanceName(change, filter))
                    .ToArray();
            }
            else if (!string.IsNullOrEmpty(filter.Actor))
            {
                string[] filterActors = playbillRepository
                    .GetActorsByNameFilter(subscription.PerformanceFilter.Actor)
                    .Select(x => x.Name)
                    .ToArray();

                performanceChanges = !filterActors.Any()
                    ? Array.Empty<PlaybillChangeEntity>()
                    : changesToSubscription.Where(change => FilterByActor(change, filterActors)).ToArray();
            }
            else if (filter.PlaybillId > 0)
            {
                performanceChanges = changesToSubscription
                    .Where(p => p.PlaybillEntity.Id == filter.PlaybillId)
                    .ToArray();
            }
            else
            {
                performanceChanges = changesToSubscription
                    .Where(p => _filterChecker.IsDataSuitable(
                        p.PlaybillEntityId,
                        p.PlaybillEntity.Performance.Name,
                        p.PlaybillEntity.Cast != null ? string.Join(',', p.PlaybillEntity.Cast.Select(c => c.Actor)) : null,
                        p.PlaybillEntity.Performance.Location.Id,
                        p.PlaybillEntity.Performance.Type.TypeName,
                        p.PlaybillEntity.When, filter)
                                && p.LastUpdate > subscription.LastUpdate
                                && (subscription.TrackingChanges & p.ReasonOfChanges) != 0
                                && subscription.TrackingChanges != 0)
                    .ToArray();
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
                await subscriptionRepository.UpdateDate(subscription.Id);
            }
        }

        Trace.TraceInformation("Process subscription finished");
        return true;
    }

    private bool FilterByPerformanceName(PlaybillChangeEntity change, PerformanceFilterEntity filter)
    {
        var playbillEntry = change.PlaybillEntity;

        return _filterChecker.IsDataSuitable(
            playbillEntry.Id,
            playbillEntry.Performance.Name,
            playbillEntry.Cast != null ? string.Join(',', playbillEntry.Cast.Select(c => c.Actor)) : null,
            playbillEntry.Performance.Location.Id,
            playbillEntry.Performance.Type.TypeName,
            playbillEntry.When, filter);
    }

    private bool FilterByActor(PlaybillChangeEntity change, string[] filterActors)
    {
        if (change.ReasonOfChanges != (int)ReasonOfChanges.CastWasChanged && change.ReasonOfChanges != (int)ReasonOfChanges.CastWasSet)
            return false;

        var addedList = change.CastAdded?.ToLower().Split(",", StringSplitOptions.RemoveEmptyEntries).ToArray();
        var removedList = change.CastRemoved?.ToLower().Split(",", StringSplitOptions.RemoveEmptyEntries).ToArray();

        bool added = addedList != null && addedList.Any(actor => filterActors.Contains(actor));
        bool removed = removedList != null && removedList.Any(actor => filterActors.Contains(actor));

        return added || removed;
    }

    private static readonly ReasonOfChanges[] ReasonToShowCast = {ReasonOfChanges.CastWasChanged, ReasonOfChanges.CastWasSet, ReasonOfChanges.Creation, ReasonOfChanges.StartSales};
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
            string formattedDate = _timeZoneService.GetLocalTime(playbillEntity.When).ToString("ddMMM HH:mm", cultureRu);


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
            if (playbillEntity.Cast != null && ReasonToShowCast.Contains(reason))
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
                    string actors = string.Join(", ", group.Value.Select(item =>
                        item.Url == CommonTags.NotDefinedTag || string.IsNullOrEmpty(item.Url)
                            ? item.Name.EscapeMessageForMarkupV2()
                            : $"[{item.Name.EscapeMessageForMarkupV2()}]({item.Url.EscapeMessageForMarkupV2()})"));

                    bool wasAdded = group.Value.Any(item => change.CastAdded.Contains(item.Name));

                    bool isPhonogram = group.Key == CommonTags.Conductor && group.Value.First().Name == CommonTags.Phonogram;

                    string character = group.Key == CommonTags.Actor || isPhonogram
                        ? string.Empty
                        : $"{group.Key} - ".EscapeMessageForMarkupV2();

                    string addedPart = wasAdded ? " (добавлен):" : string.Empty;

                    sb.AppendLine($"{character}{actors}{addedPart}");
                }

                sb.AppendLine($"Были удалены: {change.CastRemoved}");
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