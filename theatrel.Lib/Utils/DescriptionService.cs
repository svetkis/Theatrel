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

namespace theatrel.Lib.Utils;

internal class DescriptionService : IDescriptionService
{
    private readonly ITimeZoneService _timeZoneService;

    public DescriptionService(ITimeZoneService timeZoneService)
    {
        _timeZoneService = timeZoneService;
    }

    public string GetPerformanceDescription(PlaybillEntity playbillEntity, int lastMinPrice, CultureInfo culture)
    {
        string formattedDate = _timeZoneService.GetLocalTime(playbillEntity.When).ToString("ddMMM HH:mm", culture);

        string location = string.IsNullOrEmpty(playbillEntity.Performance.Location.Description)
            ? playbillEntity.Performance.Location.Name.EscapeMessageForMarkupV2()
            : playbillEntity.Performance.Location.Description.EscapeMessageForMarkupV2();

        string escapedName = playbillEntity.Performance.Name.EscapeMessageForMarkupV2();

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

        sb.AppendLine($"{escapedDate} {typeEscaped} {performanceNameString} {pricePart}");

        if (!string.IsNullOrEmpty(playbillEntity.Description))
        {
            sb.AppendLine(playbillEntity.Description.EscapeMessageForMarkupV2());
        }

        sb.AppendLine(location);

        return sb.ToString();
    }

   private bool HasUrl(string url)
   {
        return string.IsNullOrWhiteSpace(url) || CommonTags.TechnicalStateTags.Contains(url);
   }

    public string GetCastDescription(PlaybillEntity playbillEntity, string castAdded, string castRemoved)
    {
        StringBuilder sb = new ();
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
}
