using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.Common.Enums;
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
                return await AddParticularPlaybillEntrySubscription(chatInfo, message, cancellationToken);
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

        StringBuilder errorList = new StringBuilder();
        var userCommands = SubscriptionsHelper.ParseSubscriptionsCommandLine(chatInfo, message, DbService, errorList);

        return userCommands.Any();
    }

    public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
    {
        IPerformanceFilter filter = _filterService.GetFilter(chatInfo);
        var filteredPerformances = _filterService.GetFilteredPerformances(filter).OrderBy(x => x.When);

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
       
        string performancesDescription = _descriptionService.GetPerformancesMessage(
            filteredPerformances,
            CultureInfo.CreateSpecificCulture(chatInfo.Culture),
            true,
            out string performaceIdsList);

        chatInfo.Info = performaceIdsList;

        return Task.FromResult<ITgCommandResponse>(
            new TgCommandResponse(performancesDescription, keys) { IsEscaped = true });
    }

    private async Task<TgCommandResponse> AddParticularPlaybillEntrySubscription(IChatDataInfo chatInfo, string commandLine, CancellationToken cancellationToken)
    {
        StringBuilder sb = new StringBuilder();
        SubscriptionEntry[] entries = SubscriptionsHelper.ParseSubscriptionsCommandLine(chatInfo, commandLine, DbService, sb);

        if (!entries.Any())
            return new TgCommandResponse(sb.ToString());

        using var subscriptionRepository = DbService.GetSubscriptionRepository();

        var culture = CultureInfo.CreateSpecificCulture(chatInfo.Culture);

        foreach (var entry in entries)
        {
            int trackingChanges = (int)(ReasonOfChanges.StartSales
                                        | ReasonOfChanges.PriceDecreased
                                        | ReasonOfChanges.CastWasSet
                                        | ReasonOfChanges.CastWasChanged);

            SubscriptionEntity subscription = await subscriptionRepository.Create(chatInfo.UserId, trackingChanges,
                _filterService.GetFilter(entry.PlaybillEntryId), cancellationToken);

            var when = _timeZoneService.GetLocalTime(entry.When);
            sb.AppendLine(subscription != null
                ? $"Успешно добавлена подписка на {when.ToString("ddMMM HH:mm", culture)} {entry.Name}"
                : $"Не получилось добавить подписку на {when.ToString("ddMMM HH:mm", culture)} {entry.Name}");
        }

        return new TgCommandResponse(sb.ToString());
    }
}
