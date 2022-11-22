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

namespace theatrel.TLBot.Commands;

internal class GetPerformancesCommand : DialogCommandBase
{
    private readonly IFilterService _filterService;
    private readonly ITimeZoneService _timeZoneService;
    private readonly IDescriptionService _descriptionService;

    private const string DecreasePriceSubscription = "Подписаться на снижение цены на билеты";
    private const string NewInPlaybillSubscription = "Подписаться на новые спектакли и появление билетов в продаже";
    private const string CastSubscription = "Подписаться на изменения в составе исполнителей";

    private const string No = "Спасибо, не надо";

    protected override string ReturnCommandMessage { get; set; } = string.Empty;

    public override string Name => "Искать";

    public GetPerformancesCommand(
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
            new KeyboardButton(DecreasePriceSubscription),
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
            case DecreasePriceSubscription:
                trackingChanges = (int)ReasonOfChanges.PriceDecreased;
                break;
            case NewInPlaybillSubscription:
                trackingChanges = (int)(ReasonOfChanges.StartSales | ReasonOfChanges.Creation);
                break;
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
            DecreasePriceSubscription, NewInPlaybillSubscription, CastSubscription, No
        };

        if (commands.Contains(message))
            return true;

        var userCommands = SubscriptionsHelper.ParseSubscriptionsCommandLine(chatInfo, message, DbService);

        return userCommands.Any();
    }

    public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
    {
        IPerformanceFilter filter = _filterService.GetFilter(chatInfo);
        var filteredPerformances = _filterService.GetFilteredPerformances(filter).OrderBy(x => x.When);

        List<KeyboardButton> buttons = new List<KeyboardButton> { new KeyboardButton(NewInPlaybillSubscription) };
        if (filteredPerformances.Any())
        {
            buttons.Add(new KeyboardButton(DecreasePriceSubscription));
            buttons.Add(new KeyboardButton(CastSubscription));
        }

        buttons.Add(new KeyboardButton(No));

        var keys = new ReplyKeyboardMarkup(GroupKeyboardButtons(1, buttons))
        {
            OneTimeKeyboard = true,
            ResizeKeyboard = true
        };

        var response = CreatePerformancesMessage(chatInfo, filteredPerformances, filter, chatInfo.When);

        return Task.FromResult<ITgCommandResponse>(new TgCommandResponse(response, keys){ IsEscaped = true });
    }

    private async Task<TgCommandResponse> AddOnePlaybillEntrySubscription(IChatDataInfo chatInfo, string commandLine, CancellationToken cancellationToken)
    {
        StringBuilder sb = new StringBuilder();
        SubscriptionEntry[] entries = SubscriptionsHelper.ParseSubscriptionsCommandLine(chatInfo, commandLine, DbService, sb);

        if (!entries.Any())
            return new TgCommandResponse(sb.ToString());

        var culture = CultureInfo.CreateSpecificCulture(chatInfo.Culture);

        using var subscriptionRepository = DbService.GetSubscriptionRepository();

        int trackingChanges = (int)(
            ReasonOfChanges.StartSales |
            ReasonOfChanges.PriceDecreased |
            ReasonOfChanges.CastWasSet |
            ReasonOfChanges.CastWasChanged);

        foreach (var entry in entries)
        {
            SubscriptionEntity subscription = await subscriptionRepository.Create(
                chatInfo.UserId,
                trackingChanges,
                _filterService.GetFilter(entry.PlaybillEntryId),
                cancellationToken);

            var when = _timeZoneService.GetLocalTime(entry.When);

            sb.AppendLine(subscription != null
                ? $"Успешно добавлена подписка на {when.ToString("ddMMM HH:mm", culture)} {entry.Name}"
                : $"Не получилось добавить подписку на {when.ToString("ddMMM HH:mm", culture)} {entry.Name}");
        }

        return new TgCommandResponse(sb.ToString());
    }

    private string CreatePerformancesMessage(
        IChatDataInfo chatInfo,
        IEnumerable<PlaybillEntity> performances,
        IPerformanceFilter filter,
        DateTime when)
    {
        var culture = CultureInfo.CreateSpecificCulture(chatInfo.Culture);

        var stringBuilder = new StringBuilder();

        string days = filter.DaysOfWeek != null
            ? filter.DaysOfWeek.Length == 1
                ? $"день недели: {culture.DateTimeFormat.GetDayName(filter.DaysOfWeek.First())}"
                : "дни недели: " + string.Join(" или ", filter.DaysOfWeek
                    .OrderBy(d => (int)d, DaysOfWeekComparer.Create())
                    .Select(d => culture.DateTimeFormat.GetDayName(d)))
            : string.Empty;

        string types = filter.PerformanceTypes == null || !filter.PerformanceTypes.Any()
            ? "все представления"
            : string.Join(", ", filter.PerformanceTypes);

        using var playbillRepo = DbService.GetPlaybillRepository();

        bool theatresWereSelected = filter.TheatreIds != null && filter.TheatreIds.Any();

        var theatres = theatresWereSelected
            ? string.Join(", ", playbillRepo.GetTheatres().Where(x => filter.TheatreIds.Contains(x.Id)).Select(x => x.Name))
            : string.Join(", ", playbillRepo.GetTheatres().Select(x => x.Name).ToArray());

        string locations = filter.LocationIds == null || !filter.LocationIds.Any()
            ? "любая площадка"
            : string.Join(", ", filter.LocationIds.Select(x =>
            {
                var location = playbillRepo.GetLocation(x);
                return string.IsNullOrEmpty(location.Description) ? location.Name : location.Description;
            }));

        stringBuilder.AppendLine(
            string.IsNullOrEmpty(filter.PerformanceName)
                ? $"Я искал для Вас билеты на {when.ToString("MMMM yyyy", culture)} {days} на {types}. Площадка: {locations}.".EscapeMessageForMarkupV2()
                : $"Я искал для Вас билеты на \"{filter.PerformanceName}\". Площадка: {locations}.".EscapeMessageForMarkupV2());

        stringBuilder.AppendLine();

        stringBuilder.Append(_descriptionService.GetPerformancesMessage(
            performances,
            culture,
            false,
            out string performanceIdsList));

        chatInfo.Info = performanceIdsList;

        return stringBuilder.ToString();
    }
}