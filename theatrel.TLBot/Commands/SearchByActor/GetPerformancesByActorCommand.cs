using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.Common.Enums;
using theatrel.Common.FormatHelper;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.TgBot;
using theatrel.Interfaces.TimeZoneService;
using theatrel.Lib.Interfaces;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands.SearchByActor;

internal class GetPerformancesByActorCommand : DialogCommandBase
{
    private readonly IFilterService _filterService;
    private readonly ITimeZoneService _timeZoneService;
    private readonly IDescriptionService _descriptionService;

    private const string CastSubscription = "Подписка на изменения в афише с выбранным исполнителем";

    private const string No = "Спасибо, не надо";

    protected override string ReturnCommandMessage { get; set; } = string.Empty;

    public override string Name => "Искать";

    public GetPerformancesByActorCommand(
        IFilterService filterService,
        ITimeZoneService timeZoneService,
        IDescriptionService descriptionService,
        IDbService dbService)
        : base(dbService)
    {
        _filterService = filterService;
        _timeZoneService = timeZoneService;
        _descriptionService = descriptionService;

        CommandKeyboardMarkup = new ReplyKeyboardMarkup(GroupKeyboardButtons(1, new[] {
            new KeyboardButton(CastSubscription),
            new KeyboardButton(No),
        }))
        {
            OneTimeKeyboard = true,
            ResizeKeyboard = true
        };
    }

    public override async Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
    {
        int trackingChanges;

        switch (message)
        {
            case CastSubscription:
                trackingChanges = (int)(ReasonOfChanges.CastWasSet | ReasonOfChanges.CastWasChanged);
                break;
            case No:
                return new TgCommandResponse("Приятно было пообщаться. Обращайтесь еще.");
            default:
                return await AddOnePlaybillEntrySubscription(chatInfo, message, cancellationToken);
        }

        using var subscriptionRepository = DbService.GetSubscriptionRepository();

        SubscriptionEntity subscription = await subscriptionRepository.Create(chatInfo.UserId, trackingChanges,
            _filterService.GetFilter(chatInfo), cancellationToken);

        return subscription == null
            ? new TgCommandResponse("Простите, но я не смог добавить подписку.")
            : new TgCommandResponse("Подписка добавлена.");
    }

    public override bool IsMessageCorrect(IChatDataInfo chatInfo, string message)
    {
        string[] commands =
        {
            CastSubscription,
            No
        };

        if (commands.Contains(message))
            return true;

        var userCommands = ParseSubscriptionsCommandLine(chatInfo, message);

        return userCommands.Any();
    }

    public override async Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
    {
        IPerformanceFilter filter = _filterService.GetFilter(chatInfo);
        var filteredPerformances = _filterService.GetFilteredPerformances(filter);

        List<KeyboardButton> buttons = new List<KeyboardButton> 
        {
            new KeyboardButton(CastSubscription),
            new KeyboardButton(No)
        };

        var keys = new ReplyKeyboardMarkup(GroupKeyboardButtons(1, buttons))
        {
            OneTimeKeyboard = true,
            ResizeKeyboard = true
        };

        return new TgCommandResponse(await CreatePerformancesMessage(chatInfo, filteredPerformances, filter, chatInfo.When, chatInfo.Culture), keys) { IsEscaped = true };
    }

    private async Task<TgCommandResponse> AddOnePlaybillEntrySubscription(IChatDataInfo chatInfo, string commandLine, CancellationToken cancellationToken)
    {
        StringBuilder sb = new StringBuilder();
        SubscriptionEntry[] entries = ParseSubscriptionsCommandLine(chatInfo, commandLine, sb);

        if (!entries.Any())
            return new TgCommandResponse(sb.ToString());

        using var subscriptionRepository = DbService.GetSubscriptionRepository();

        foreach (var entry in entries)
        {
            int trackingChanges = 0;

            switch (entry.SubscriptionType)
            {
                case 1:
                    trackingChanges = (int)ReasonOfChanges.StartSales;
                    break;
                case 2:
                    trackingChanges = (int)ReasonOfChanges.PriceDecreased;
                    break;
                case 3:
                    trackingChanges = (int)(ReasonOfChanges.CastWasSet | ReasonOfChanges.CastWasChanged);
                    break;

                case 4:
                    trackingChanges = (int)(ReasonOfChanges.StartSales | ReasonOfChanges.PriceDecreased |
                                            ReasonOfChanges.CastWasSet | ReasonOfChanges.CastWasChanged);
                    break;
            }

            SubscriptionEntity subscription = await subscriptionRepository.Create(chatInfo.UserId, trackingChanges,
                _filterService.GetFilter(entry.PlaybillEntryId), cancellationToken);

            var when = _timeZoneService.GetLocalTime(entry.When);

            sb.AppendLine(subscription != null
                ? $"Успешно добавлена подписка на {when:ddMMM HH:mm} {entry.Name}"
                : $"Не вышло добавить подписку на {when:ddMMM HH:mm} {entry.Name}");
        }

        return new TgCommandResponse(sb.ToString());
    }

    private class SubscriptionEntry
    {
        public int PlaybillEntryId;
        public int SubscriptionType;
        public DateTime When;
        public string Name;
    }

    private SubscriptionEntry[] ParseSubscriptionsCommandLine(IChatDataInfo chatInfo, string commandLine, StringBuilder sb = null)
    {
        if (string.IsNullOrEmpty(chatInfo.Info))
            return Array.Empty<SubscriptionEntry>();

        string[] splitIds = chatInfo.Info.Split(',');

        List<SubscriptionEntry> entriesList = new List<SubscriptionEntry>();
        string[] parsedCommands = commandLine.Split(' ');

        foreach (var cmd in parsedCommands)
        {
            string[] split = cmd.Split('-');
            if (split.Length != 2)
            {
                sb?.AppendLine($"Не распознанная команда {cmd}");
                continue;
            }

            string playbillEntryIdIdx = split[0];
            string subscriptionType = split[1];

            if (!int.TryParse(playbillEntryIdIdx, out int entryIdIdx))
            {
                sb?.AppendLine($"Ошибка парсинга {cmd}");
                continue;
            }

            int entryId = splitIds.Length >= entryIdIdx ? int.Parse(splitIds[entryIdIdx - 1]) : -1;

            if (!int.TryParse(subscriptionType, out int subscriptionCode))
            {
                sb?.AppendLine($"Ошибка парсинга {cmd}");
                continue;
            }

            entriesList.Add(new SubscriptionEntry { PlaybillEntryId = entryId, SubscriptionType = subscriptionCode });
        }

        ResolvePlaybillInfo(entriesList);
        return RemoveWrongSubscriptionCommands(entriesList.ToArray(), sb);
    }

    private SubscriptionEntry[] RemoveWrongSubscriptionCommands(SubscriptionEntry[] entriesList, StringBuilder sb)
    {
        if (!entriesList.Any())
            return Array.Empty<SubscriptionEntry>();

        List<SubscriptionEntry> wrongList = new List<SubscriptionEntry>();
        foreach (var entry in entriesList)
        {
            if (string.IsNullOrEmpty(entry.Name))
            {
                sb?.AppendLine("Не найден спектакль");
                wrongList.Add(entry);
                continue;
            }

            if (entry.SubscriptionType < 1 || entry.SubscriptionType > 4)
            {
                var when = _timeZoneService.GetLocalTime(entry.When);

                sb?.AppendLine($"Неправильный код подписки {entry.SubscriptionType} для спектакля {when:ddMMM HH:mm} {entry.Name}");
                wrongList.Add(entry);
            }
        }

        return entriesList.Except(wrongList).ToArray();
    }

    private void ResolvePlaybillInfo(IList<SubscriptionEntry> entriesList)
    {
        if (!entriesList.Any())
            return;

        using var playbillRepository = DbService.GetPlaybillRepository();

        foreach (var entry in entriesList)
        {
            var pbEntity = playbillRepository.GetPlaybillEntryWithPerformanceData(entry.PlaybillEntryId);
            entry.Name = pbEntity?.Performance.Name;
            entry.When = pbEntity?.When ?? DateTime.UtcNow;
        }
    }

    private readonly string envelopeEmoji = Encoding.UTF8.GetString(new byte[] { 0xE2, 0x9C, 0x89 });

    private Task<string> CreatePerformancesMessage(IChatDataInfo chatInfo, PlaybillEntity[] performances, IPerformanceFilter filter, DateTime when, string culture)
    {
        var cultureRu = CultureInfo.CreateSpecificCulture(culture);

        var sb = new StringBuilder();

        int i = 0;
        StringBuilder savedInfo = new StringBuilder();
        foreach (PlaybillEntity item in performances.OrderBy(item => item.When).Where(item => item.When > DateTime.UtcNow))
        {
            if (!item.Changes.Any())
                continue;

            savedInfo.Append($"{item.Id},");

            var lastChange = item.Changes.OrderBy(ch => ch.LastUpdate).Last();
            if (lastChange.ReasonOfChanges == (int)ReasonOfChanges.WasMoved)
                continue;

            int minPrice = lastChange.MinPrice;

            string subscriptionIndexPart = $"{envelopeEmoji}Индекс для подписки {++i}";
            sb.AppendLine(subscriptionIndexPart);

            sb.AppendLine(_descriptionService.GetPerformanceDescription(item, minPrice, cultureRu));

            string cast = _descriptionService.GetCastDescription(item, null, null);
  
            if (!string.IsNullOrEmpty(cast.ToString()))
                sb.AppendLine(cast.ToString());
        }

        if (!performances.Any())
            return Task.FromResult("Увы, я ничего не нашел. Можете подписаться и я пришлю Вам сообщение про новые спектакли с этим исполнителем.".EscapeMessageForMarkupV2());

        sb.AppendLine("Для подписки на конкретный спектакль напишите индекс подписки-код подписки, например: 5-1".EscapeMessageForMarkupV2());
        sb.AppendLine("Или сразу несколько кодов и подписок например: 5-1 6-4 10-2".EscapeMessageForMarkupV2());
        sb.AppendLine("Коды подписки: 1 появление билетов в продаже, 2 снижение цены, 3 изменения состава исполнителей, 4 все события".EscapeMessageForMarkupV2());

        chatInfo.Info = savedInfo.ToString();

        return Task.FromResult(sb.ToString());
    }
}
