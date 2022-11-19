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
using theatrel.Lib.Interfaces;
using theatrel.TLBot.Interfaces;

namespace theatrel.Subscriptions;

public class SubscriptionProcessor : ISubscriptionProcessor
{
    private readonly ITgBotService _telegramService;
    private readonly IDbService _dbService;
    private readonly IFilterService _filterService;
    private readonly ITimeZoneService _timeZoneService;
    private readonly IDescriptionService _descriptionSevice;

    public SubscriptionProcessor(
        ITgBotService telegramService,
        IFilterService filterService,
        ITimeZoneService timeZoneService,
        IDescriptionService descriptionService,
        IDbService dbService)
    {
        _telegramService = telegramService;
        _dbService = dbService;
        _filterService = filterService;
        _descriptionSevice = descriptionService;
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
            PlaybillChangeEntity[] subscriptionChanges = _filterService
                    .GetFilteredChanges(changes, subscription)
                    .OrderBy(p => p.LastUpdate)
                    .ToArray();

            if (!subscriptionChanges.Any())
                continue;

            if (!messagesDictionary.ContainsKey(subscription.TelegramUserId))
                messagesDictionary.Add(subscription.TelegramUserId, new Dictionary<int, List<PlaybillChangeEntity>>());

            Dictionary<int, List<PlaybillChangeEntity>> changesToUser = messagesDictionary[subscription.TelegramUserId];
            foreach (var change in subscriptionChanges)
            {
                if (!changesToUser.ContainsKey(change.PlaybillEntityId))
                {
                    changesToUser[change.PlaybillEntityId] = new List<PlaybillChangeEntity> {change};
                    continue;
                }

                changesToUser[change.PlaybillEntityId].Add(change);
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

    private static readonly ReasonOfChanges[] ReasonToShowCast = {ReasonOfChanges.CastWasChanged, ReasonOfChanges.CastWasSet, ReasonOfChanges.Creation, ReasonOfChanges.StartSales};

    private string GetChangesDescription(PlaybillChangeEntity[] changes)
    {
        StringBuilder sb = new StringBuilder();

        var cultureRu = CultureInfo.CreateSpecificCulture("ru");

        ReasonOfChanges reason = (ReasonOfChanges) changes.First().ReasonOfChanges;
        sb.AppendLine(GetReasonDescription(reason));

        foreach (PlaybillChangeEntity change in changes)
        {
            GetChangeDescription(change, sb, cultureRu);
        }

        return sb.ToString();
    }

    private string GetReasonDescription(ReasonOfChanges reason)
    {
        switch (reason)
        {
            case ReasonOfChanges.Creation: 
                return "Новое в афише:";
            case ReasonOfChanges.PriceDecreased:
                return "Снижена цена:";
            case ReasonOfChanges.PriceIncreased:
                return "Билеты подорожали:";
            case ReasonOfChanges.StartSales:
                return "Появились в продаже билеты:";
            case ReasonOfChanges.StopSales:
                return "Стоп продаж:";
            case ReasonOfChanges.WasMoved:
                return "Перенесены на другую дату:";
            case ReasonOfChanges.StopSale:
                return "Закончились билеты:";
            case ReasonOfChanges.CastWasSet:
                return "Объявлен состав исполнителей:";
            case ReasonOfChanges.CastWasChanged:
                return "Состав исполнителей изменен:";
            default:
                return string.Empty;
        }
    }

    private Dictionary<ReasonOfChanges, string> _reasonToEmoji = new Dictionary<ReasonOfChanges, string>() 
    {
        { ReasonOfChanges.Creation, Encoding.UTF8.GetString(new byte[] { 0xF0, 0x9F, 0x86, 0x95 })},
        { ReasonOfChanges.PriceDecreased, Encoding.UTF8.GetString(new byte[] { 0xE2, 0xAC, 0x87 })},
        { ReasonOfChanges.PriceIncreased, Encoding.UTF8.GetString(new byte[] { 0xE2, 0xAC, 0x86 })},
        { ReasonOfChanges.StartSales, Encoding.UTF8.GetString(new byte[] { 0xF0, 0x9F, 0x94, 0x94 })},
        { ReasonOfChanges.StopSales, Encoding.UTF8.GetString(new byte[] { 0xE2, 0x9D, 0x8C })},
        { ReasonOfChanges.WasMoved, Encoding.UTF8.GetString(new byte[] { 0xE2, 0x9D, 0x97 })},
        { ReasonOfChanges.StopSale, Encoding.UTF8.GetString(new byte[] { 0xE2, 0x9D, 0x8C })},
        { ReasonOfChanges.CastWasSet, Encoding.UTF8.GetString(new byte[] { 0xF0, 0x9F, 0x94, 0x84 })},
        { ReasonOfChanges.CastWasChanged, Encoding.UTF8.GetString(new byte[] { 0xF0, 0x9F, 0x94, 0x84 })},
    };

    private void GetChangeDescription(PlaybillChangeEntity change, StringBuilder sb, CultureInfo culture)
    {
        PlaybillEntity playbillEntity = change.PlaybillEntity;

        string performanceDescription = _descriptionSevice.GetPerformanceDescription(playbillEntity, change.MinPrice, culture);
        
        string emojie = _reasonToEmoji[(ReasonOfChanges)change.ReasonOfChanges];

        sb.AppendLine($"{emojie} {performanceDescription}");

        if (playbillEntity.Cast != null && ReasonToShowCast.Contains((ReasonOfChanges)change.ReasonOfChanges))
        {
            string cast = _descriptionSevice.GetCastDescription(playbillEntity, change.CastAdded, change.CastRemoved);
            if (!string.IsNullOrEmpty(cast))
            {
                sb.AppendLine(cast);
            }
        }

        sb.AppendLine();
    }

    private async Task<bool> SendMessages(long tgUserId, PlaybillChangeEntity[] changes)
    {
        var groups = changes
            .GroupBy(change => change.ReasonOfChanges)
            .Select(group => group.OrderBy(x => x.PlaybillEntity.When).ToArray());

        string message = string.Join(Environment.NewLine, groups.Select(GetChangesDescription));

        return await _telegramService.SendEscapedMessageAsync(tgUserId, message, CancellationToken.None);
    }
}