using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Common.Enums;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Subscriptions;
using theatrel.Interfaces.VkIntegration;
using theatrel.Lib.Interfaces;
using theatrel.TLBot.Interfaces;

namespace theatrel.Subscriptions;

public class SubscriptionProcessor : ISubscriptionProcessor
{
    private readonly ITgBotService _telegramService;
    private readonly IVkIntegration _vkIntegration;
    private readonly IDbService _dbService;
    private readonly IFilterService _filterService;
    private readonly IDescriptionService _descriptionService;

    public SubscriptionProcessor(
        ITgBotService telegramService,
        IFilterService filterService,
        IDescriptionService descriptionService,
        IVkIntegration vkIntegration,
        IDbService dbService)
    {
        _telegramService = telegramService;
        _dbService = dbService;
        _filterService = filterService;
        _descriptionService = descriptionService;
        _vkIntegration = vkIntegration;
    }

    public async Task<bool> ProcessSubscriptions()
    {
        Trace.TraceInformation("Process subscriptions started");

        using ISubscriptionsRepository subscriptionRepository = _dbService.GetSubscriptionRepository();
        var subscriptions = subscriptionRepository.GetAllWithFilter().ToArray();

        if (!subscriptions.Any())
            return true;

        SubscriptionEntity[] changedBasedSubscription = subscriptions.Where(s => s.SubscriptionType == 0).ToArray();

        await ProcessChangedBasedSubscriptions(changedBasedSubscription, subscriptionRepository);

        SubscriptionEntity[] endedPerformanceSubscription = subscriptions.Where(s => s.SubscriptionType == 1).ToArray();
        await ProcessEndedPerformanceSubscriptions(endedPerformanceSubscription, subscriptionRepository);

        var subscriptionsVk = subscriptionRepository.GetAllWithFilterVk().ToArray();

        if (!subscriptions.Any())
            return true;

        var changedBasedSubscriptionVk = subscriptionsVk
            .Where(s => s.SubscriptionType == 0)
            .ToArray();

        await ProcessChangedBasedSubscriptionsVk(changedBasedSubscriptionVk, subscriptionRepository);
        
        Trace.TraceInformation("Process subscription finished");
        return true;
    }

    private async Task ProcessEndedPerformanceSubscriptions(SubscriptionEntity[] subscriptions, ISubscriptionsRepository subscriptionRepository)
    {
        using IPlaybillRepository playbillRepository = _dbService.GetPlaybillRepository();
        var outdated = playbillRepository.GetOutdatedPlaybillForArchive();

        if (!subscriptions.Any())
        {
            foreach (var outdatedEntry in outdated)
            {
                await playbillRepository.SetPlaybillReadyToDelete(outdatedEntry);
            }
            return;
        }

        var cultureRu = CultureInfo.CreateSpecificCulture("ru");

        var descriptions = outdated
            .Select(x => {
                string performance = _descriptionService.GetTgPerformanceDescription(x, 0, cultureRu, null);
                string cast = _descriptionService.GetTgCastDescription(x, null, null);

                return new { Entry = x, Description = $"{performance}{Environment.NewLine}{cast}" };
            })
            .ToArray();

        foreach (var archiveEntry in descriptions)
        {
            foreach (SubscriptionEntity subscription in subscriptions)
            {
                var sent = await _telegramService.SendEscapedMessageAsync(
                        subscription.TelegramUserId,
                        archiveEntry.Description,
                        CancellationToken.None);

            }

            await playbillRepository.SetPlaybillReadyToDelete(archiveEntry.Entry);
        }
    }

    private async Task ProcessChangedBasedSubscriptions(SubscriptionEntity[] subscriptions, ISubscriptionsRepository subscriptionRepository)
    {
        if (!subscriptions.Any())
            return;

        var messagesDictionary = GetChangesToUsers(subscriptions, subscriptionRepository);

        foreach (KeyValuePair<long, Dictionary<int, List<PlaybillChangeEntity>>> userUpdates in messagesDictionary)
        {
            var changesToProcess = userUpdates.Value.SelectMany(d => d.Value.ToArray()).Distinct().ToArray();
            if (!await SendMessages(userUpdates.Key, changesToProcess))
                continue;

            //if message was sent we should update LastUpdate for users subscriptions
            foreach (var subscription in subscriptions.Where(s => s.TelegramUserId == userUpdates.Key))
            {
                await subscriptionRepository.UpdateDate(subscription.Id);
            }
        }
    }

    private async Task ProcessChangedBasedSubscriptionsVk(VkSubscriptionEntity[] subscriptions, ISubscriptionsRepository subscriptionRepository)
    {
        if (!subscriptions.Any())
            return;

        var messagesDictionary = GetChangesToUsersVk(subscriptions, subscriptionRepository);

        foreach (KeyValuePair<long, Dictionary<int, List<PlaybillChangeEntity>>> userUpdates in messagesDictionary)
        {
            var changesToProcess = userUpdates.Value.SelectMany(d => d.Value.ToArray()).Distinct().ToArray();
            if (!await SendVkMessages(userUpdates.Key, changesToProcess))
                continue;

            //if message was sent we should update LastUpdate for users subscriptions
            foreach (var subscription in subscriptions.Where(s => s.VkId == userUpdates.Key))
            {
                await subscriptionRepository.UpdateDate(subscription.Id);
            }
        }
    }

    private Dictionary<long, Dictionary<int, List<PlaybillChangeEntity>>> GetChangesToUsers(
            SubscriptionEntity[] subscriptions,
            ISubscriptionsRepository subscriptionRepository)
    {
        using IPlaybillRepository playbillRepository =
            subscriptions.Any(s => !string.IsNullOrEmpty(s.PerformanceFilter.Actor))
                ? _dbService.GetPlaybillRepository()
                : null;

        DateTime lastSubscriptionsUpdate = subscriptions.Min(s => s.LastUpdate).ToUniversalTime();

        PlaybillChangeEntity[] changes = subscriptionRepository.GetFreshChanges(lastSubscriptionsUpdate);

        Dictionary<long, Dictionary<int, List<PlaybillChangeEntity>>> messagesDictionary =
            new Dictionary<long, Dictionary<int, List<PlaybillChangeEntity>>>();

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
                    changesToUser[change.PlaybillEntityId] = new List<PlaybillChangeEntity> { change };
                    continue;
                }

                changesToUser[change.PlaybillEntityId].Add(change);
            }
        }

        return messagesDictionary;
    }

    private Dictionary<long, Dictionary<int, List<PlaybillChangeEntity>>> GetChangesToUsersVk(
        VkSubscriptionEntity[] subscriptions,
        ISubscriptionsRepository subscriptionRepository)
    {
        using IPlaybillRepository playbillRepository =
            subscriptions.Any(s => !string.IsNullOrEmpty(s.PerformanceFilter.Actor))
                ? _dbService.GetPlaybillRepository()
                : null;

        DateTime lastSubscriptionsUpdate = subscriptions.Min(s => s.LastUpdate).ToUniversalTime();

        PlaybillChangeEntity[] changes = subscriptionRepository.GetFreshChanges(lastSubscriptionsUpdate);

        Dictionary<long, Dictionary<int, List<PlaybillChangeEntity>>> messagesDictionary =
            new Dictionary<long, Dictionary<int, List<PlaybillChangeEntity>>>();

        foreach (VkSubscriptionEntity subscription in subscriptions)
        {
            PlaybillChangeEntity[] subscriptionChanges = _filterService
                .GetVkFilteredChanges(changes, subscription)
                .OrderBy(p => p.LastUpdate)
                .ToArray();

            if (!subscriptionChanges.Any())
                continue;

            if (!messagesDictionary.ContainsKey(subscription.VkId))
                messagesDictionary.Add(subscription.VkId, new Dictionary<int, List<PlaybillChangeEntity>>());

            Dictionary<int, List<PlaybillChangeEntity>> changesToUser = messagesDictionary[subscription.VkId];

            foreach (var change in subscriptionChanges)
            {
                if (!changesToUser.ContainsKey(change.PlaybillEntityId))
                {
                    changesToUser[change.PlaybillEntityId] = new List<PlaybillChangeEntity> { change };
                    continue;
                }

                changesToUser[change.PlaybillEntityId].Add(change);
            }
        }

        return messagesDictionary;
    }

    private static readonly ReasonOfChanges[] ReasonToShowCast = {ReasonOfChanges.CastWasChanged, ReasonOfChanges.CastWasSet, ReasonOfChanges.Creation, ReasonOfChanges.StartSales};

    private string GetChangesDescription(PlaybillChangeEntity[] changes)
    {
        StringBuilder sb = new StringBuilder();

        var cultureRu = CultureInfo.CreateSpecificCulture("ru");

        PlaybillEntity playbillEntity = changes.First().PlaybillEntity;

        int minPrice = changes
            .OrderBy(x => x.LastUpdate)
            .Last()
            .MinPrice;

        string performanceDescription = _descriptionService.GetTgPerformanceDescription(
            playbillEntity,
            minPrice,
            cultureRu,
            changes
                .Select(x => (ReasonOfChanges)x.ReasonOfChanges == ReasonOfChanges.CastWasSet
                    ? ReasonOfChanges.CastWasChanged
                    : (ReasonOfChanges)x.ReasonOfChanges)
                .Distinct()
                .ToArray());

        sb.AppendLine(performanceDescription);

        var lastCastUpdate = changes
            .Where(x => ReasonToShowCast.Contains((ReasonOfChanges)x.ReasonOfChanges))
            .MaxBy(x => x.LastUpdate);

        if (lastCastUpdate != null)
        {
            string cast = _descriptionService.GetTgCastDescription(playbillEntity, lastCastUpdate.CastAdded, lastCastUpdate.CastRemoved);
            if (!string.IsNullOrEmpty(cast))
            {
                sb.Append(cast);
            }
        }

        sb.AppendLine();
        return sb.ToString();
    }

    private string GetChangesDescriptionVk(PlaybillChangeEntity[] changes)
    {
        StringBuilder sb = new StringBuilder();

        var cultureRu = CultureInfo.CreateSpecificCulture("ru");

        PlaybillEntity playbillEntity = changes.First().PlaybillEntity;

        int minPrice = changes
            .OrderBy(x => x.LastUpdate)
            .Last()
            .MinPrice;

        string performanceDescription = _descriptionService.GetVkPerformanceDescription(
            playbillEntity,
            minPrice,
            cultureRu,
            changes
                .Select(x => (ReasonOfChanges)x.ReasonOfChanges == ReasonOfChanges.CastWasSet
                    ? ReasonOfChanges.CastWasChanged
                    : (ReasonOfChanges)x.ReasonOfChanges)
                .Distinct()
                .ToArray());

        sb.AppendLine(performanceDescription);

        var lastCastUpdate = changes
            .Where(x => ReasonToShowCast.Contains((ReasonOfChanges)x.ReasonOfChanges))
            .MaxBy(x => x.LastUpdate);

        if (lastCastUpdate != null)
        {
            string cast = _descriptionService.GetVkCastDescription(playbillEntity, lastCastUpdate.CastAdded, lastCastUpdate.CastRemoved);
            if (!string.IsNullOrEmpty(cast))
            {
                sb.Append(cast);
            }
        }

        sb.AppendLine();
        return sb.ToString();
    }

    private Dictionary<ReasonOfChanges, string> _reasonToEmoji = new() 
    {
        { ReasonOfChanges.Creation, Encoding.UTF8.GetString("\ud83c\udd95"u8.ToArray())},
        { ReasonOfChanges.PriceDecreased, Encoding.UTF8.GetString("\u2b07"u8.ToArray())},
        { ReasonOfChanges.PriceIncreased, Encoding.UTF8.GetString("\u2b06"u8.ToArray())},
        { ReasonOfChanges.StartSales, Encoding.UTF8.GetString("\ud83d\udd14"u8.ToArray())},
        { ReasonOfChanges.StopSales, Encoding.UTF8.GetString("\u274c"u8.ToArray())},
        { ReasonOfChanges.WasMoved, Encoding.UTF8.GetString("\u2757"u8.ToArray())},
        { ReasonOfChanges.StopSale, Encoding.UTF8.GetString("\u274c"u8.ToArray())},
        { ReasonOfChanges.CastWasSet, Encoding.UTF8.GetString("\ud83d\udd04"u8.ToArray())},
        { ReasonOfChanges.CastWasChanged, Encoding.UTF8.GetString("\ud83d\udd04"u8.ToArray())},
    };

    private async Task<bool> SendMessages(long tgUserId, PlaybillChangeEntity[] changes)
    {
        var performanceChangesGroups = changes
            .GroupBy(change => change.PlaybillEntityId)
            .OrderBy(group => group.First().PlaybillEntity.When)
            .Select(x => x.ToArray())
            .ToArray();

        string message = string.Join(string.Empty, performanceChangesGroups.Select(GetChangesDescription));

        return await _telegramService.SendEscapedMessageAsync(tgUserId, message, CancellationToken.None);
    }

    private async Task<bool> SendVkMessages(long vkId, PlaybillChangeEntity[] changes)
    {
        var performanceChangesGroups = changes
            .GroupBy(change => change.PlaybillEntityId)
            .OrderBy(group => group.First().PlaybillEntity.When)
            .Select(x => x.ToArray())
            .ToArray();

        string message = string.Join(string.Empty, performanceChangesGroups.Select(GetChangesDescriptionVk));

        return await _vkIntegration.SendMessage(vkId, message);
    }
}