using AngleSharp.Dom;
using System;
using System.Linq;
using theatrel.Common.FormatHelper;
using theatrel.Common;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.TimeZoneService;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using theatrel.Lib.Interfaces;
using theatrel.Common.Enums;

namespace theatrel.Lib.DescriptionService;

internal class DescriptionService : IDescriptionService
{
    private readonly ITimeZoneService _timeZoneService;

    private Dictionary<ReasonOfChanges, string> _reasonToEmoji = new Dictionary<ReasonOfChanges, string>()
    {
        { ReasonOfChanges.Creation, "🆕"},
        { ReasonOfChanges.PriceDecreased, "⬇️"},
        { ReasonOfChanges.PriceIncreased, "⬆️"},
        { ReasonOfChanges.StartSales, "🔔"},
        { ReasonOfChanges.StopSales, "❌"},
        { ReasonOfChanges.WasMoved, "❗"},
        { ReasonOfChanges.StopSale, "❌"},
        { ReasonOfChanges.CastWasSet, "🔄"},
        { ReasonOfChanges.CastWasChanged, "🔄"},
    };

    public DescriptionService(ITimeZoneService timeZoneService)
    {
        _timeZoneService = timeZoneService;
    }

    public string GetPerformanceDescription(
        PlaybillEntity playbillEntity,
        int lastMinPrice,
        CultureInfo culture,
        ReasonOfChanges[] reasonOfChanges)
    {
        string formattedDate = _timeZoneService.GetLocalTime(playbillEntity.When).ToString("ddMMM HH:mm", culture);

        string location = string.IsNullOrEmpty(playbillEntity.Performance.Location.Description)
            ? playbillEntity.Performance.Location.Name.EscapeMessageForMarkupV2()
            : playbillEntity.Performance.Location.Description.EscapeMessageForMarkupV2();

        string escapedName = $"\"{playbillEntity.Performance.Name}\"".EscapeMessageForMarkupV2();

        bool noTicketsUrl = string.IsNullOrWhiteSpace(playbillEntity.TicketsUrl) ||
                            CommonTags.TechnicalStateTags.Contains(playbillEntity.TicketsUrl);

        string pricePart = lastMinPrice == 0 || noTicketsUrl
            ? string.Empty
            : $"от [{lastMinPrice}]({playbillEntity.TicketsUrl.EscapeMessageForMarkupV2()})";

        string performanceNameString = HasUrl(playbillEntity.Url)
            ? escapedName
            : $"[{escapedName}]({playbillEntity.Url.EscapeMessageForMarkupV2()})";

        string typeEscaped = playbillEntity.Performance.Type.TypeName.EscapeMessageForMarkupV2();

        string escapedDate = formattedDate.EscapeMessageForMarkupV2();

        var sb = new StringBuilder();

        if (reasonOfChanges != null && reasonOfChanges.Any())
        {
            foreach (var change in reasonOfChanges)
                sb.Append(_reasonToEmoji[change]);

            sb.Append(" ");
        }

        sb.Append($"{escapedDate} {typeEscaped} {performanceNameString} {pricePart} ");

        sb.Append(location);

        if (!string.IsNullOrEmpty(playbillEntity.Description))
        {
            sb.AppendLine();
            sb.Append(playbillEntity.Description.EscapeMessageForMarkupV2());
            sb.Append(" ");
        }

        return sb.ToString();
    }

    private bool HasUrl(string url)
    {
        return string.IsNullOrWhiteSpace(url) || CommonTags.TechnicalStateTags.Contains(url);
    }

    public string GetCastDescription(PlaybillEntity playbillEntity, string castAdded, string castRemoved)
    {
        StringBuilder sb = new();
        IDictionary<string, IList<ActorEntity>> actorsDictionary = new Dictionary<string, IList<ActorEntity>>();

        var sortedCast = playbillEntity.Cast.OrderBy(x => x, new ActorComparer());

        foreach (var item in sortedCast)
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

            bool wasAdded = group.Value.Any(item => castAdded?.Contains(item.Name) ?? false);

            bool isPhonogram = group.Key == CommonTags.Conductor && group.Value.First().Name == CommonTags.Phonogram;

            string character = group.Key == CommonTags.Actor || isPhonogram
                ? string.Empty
                : $"{group.Key} - ".EscapeMessageForMarkupV2();

            string addedPart = wasAdded ? " (+)".EscapeMessageForMarkupV2() : string.Empty;

            sb.AppendLine($"{character}{actors}{addedPart}");
        }

        if (!string.IsNullOrEmpty(castRemoved))
            sb.AppendLine($"Были удалены: {castRemoved.Replace(",", ", ")}".EscapeMessageForMarkupV2());

        return sb.ToString();
    }

    public string GetPerformancesMessage(
        IEnumerable<PlaybillEntity> performances,
        CultureInfo culture,
        bool includeCast,
        out string performanceIdsList)
    {
        if (!performances.Any())
        {
            performanceIdsList = null;
            return "К сожалению ничего не нашлось. Подпишитесь и я пришлю Вам сообщение про новые спектакли по этому фильтру.".EscapeMessageForMarkupV2();
        }

        performanceIdsList = string.Join(",", performances.Select(x => x.Id));

        int i = 0;
        var sb = new StringBuilder();

        foreach (PlaybillEntity item in performances)
        {
            var lastChange = item.Changes.OrderBy(ch => ch.LastUpdate).Last();

            var performanceString = GetPerformanceDescription(
                item,
                lastChange.MinPrice,
                culture,
                Array.Empty<ReasonOfChanges>());

            sb.AppendLine(performanceString);

            if (includeCast)
            {
                string cast = GetCastDescription(item, null, null);

                if (!string.IsNullOrEmpty(cast.ToString()))
                    sb.Append(cast.ToString());
            }

            sb.AppendLine($"☝️Индекс для подписки {++i}");
            sb.AppendLine();
        }

        sb.AppendLine("Для подписки на конкретный спектакль напишите индекс подписки, например: 5".EscapeMessageForMarkupV2());
        sb.AppendLine("Или сразу несколько индексов: 5, 6, 10".EscapeMessageForMarkupV2());

        return sb.ToString();
    }
}
