using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.Common.FormatHelper;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.DataAccess.Structures.Interfaces;
using theatrel.Interfaces.TgBot;
using theatrel.Interfaces.TimeZoneService;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands.Subscriptions;

internal class ManageSubscriptionsCommand : DialogCommandBase
{
    private const string DeleteAll = "Удалить все";
    private const string DeleteMany = "Удалить";
    private const string NothingTodo = "Оставить как есть";
    private readonly ITimeZoneService _timeZoneService;

    protected override string ReturnCommandMessage { get; set; } = string.Empty;

    public override string Name => "Редактировать подписки";

    public ManageSubscriptionsCommand(IDbService dbService, ITimeZoneService timeZoneService) : base(dbService)
    {
        _timeZoneService = timeZoneService;
    }

    public override bool IsMessageCorrect(IChatDataInfo chatInfo, string message)
    {
        string trimMsg = message.Trim();
        if (string.Equals(trimMsg, DeleteAll, StringComparison.InvariantCultureIgnoreCase))
            return true;

        if (string.Equals(trimMsg, NothingTodo, StringComparison.InvariantCultureIgnoreCase))
            return true;

        if (new[] { DeleteMany }.Any(s => trimMsg.StartsWith(s, StringComparison.InvariantCultureIgnoreCase)))
            return true;

        return false;
    }

    private static int GetInt(string msg)
    {
        if (int.TryParse(msg, out var value))
        {
            return value - 1;
        }

        return -1;
    }

    private static int[] GetInts(string msg)
    {
        if (string.IsNullOrEmpty(msg))
            return Array.Empty<int>();

        string[] splitData = msg.Split(",");

        return splitData.Select(s => GetInt(s.Trim())).ToArray();
    }

    public override async Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
    {
        string trimMsg = message.Trim().ToLower();

        bool isDeleteAll = string.Equals(trimMsg, DeleteAll, StringComparison.InvariantCultureIgnoreCase);
        bool isDeleteMany = trimMsg.StartsWith(DeleteMany, StringComparison.InvariantCultureIgnoreCase);

        if (!(isDeleteAll || isDeleteMany))
            return new TgCommandResponse(null);

        using var subscriptionRepository = DbService.GetSubscriptionRepository();

        SubscriptionEntity[] toDelete = null;

        if (isDeleteAll)
            toDelete = subscriptionRepository.GetUserSubscriptions(chatInfo.UserId);

        bool isDeleteOnlyOne = false;

        if (isDeleteMany && !isDeleteAll)
        {
            int[] indexes = GetInts(trimMsg.Substring(DeleteMany.Length + 1));
            isDeleteOnlyOne = indexes.Length == 1;
            var subscriptions = subscriptionRepository.GetUserSubscriptions(chatInfo.UserId);
            if (indexes.Any(i => i > subscriptions.Length - 1 || i < 0))
            {
                return new TgCommandResponse("Произошла ошибка. Не правильный индекс подписки.");
            }

            toDelete = subscriptions.Select((s, i) => new { idx = i, subscription = s })
                .Where(d => indexes.Contains(d.idx)).Select(d => d.subscription).ToArray();
        }

        if (null == toDelete) // no command
        {
            return new TgCommandResponse(null);
        }

        bool result = await subscriptionRepository.DeleteRange(toDelete);

        if (!result)
            return new TgCommandResponse("Произошла ошибка при удалении.");

        return new TgCommandResponse(isDeleteOnlyOne ? "Подписка была успешно удалена" : "Ваши подписки были успешно удалены.")
        {
            NeedToRepeat = !isDeleteAll
        };
    }

    private LocationsEntity[] GetLocations()
    {
        using var repo = DbService.GetPlaybillRepository();
       
        var theatreIds = repo.GetTheatres().OrderBy(x => x.Id).Select(x => x.Id).ToArray();

        return theatreIds
            .SelectMany(theatreId => repo.GetLocationsList(theatreId).OrderBy(x => x.Id))
            .ToArray();
    }

    private string[] GetTheatres(int[] ids)
    {
        using var repo = DbService.GetPlaybillRepository();

        var theatres = repo.GetTheatres();

        bool theatresWereSelected = ids != null && ids.Any();
        var theatreIds = theatresWereSelected
            ? ids
            : repo.GetTheatres().OrderBy(x => x.Id).Select(x => x.Id).ToArray();

        return theatreIds
                .Select(theatreId => theatres.FirstOrDefault(x => x.Id == theatreId).Name)
                .ToArray();
    }

    private string GetLocationDescription(LocationsEntity location)
    {
        return string.IsNullOrEmpty(location.Description) ? location.Name : location.Description;
    } 

    public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
    {
        using var subscriptionRepository = DbService.GetSubscriptionRepository();
        SubscriptionEntity[] subscriptions = subscriptionRepository.GetUserSubscriptions(chatInfo.UserId);

        using var playbillRepository = DbService.GetPlaybillRepository();

        if (!subscriptions.Any())
            return Task.FromResult<ITgCommandResponse>(new TgCommandResponse("У вас нет подписок."));

        List<KeyboardButton> buttons = new List<KeyboardButton>();

        var culture = CultureInfo.CreateSpecificCulture(chatInfo.Culture);

        var stringBuilder = new StringBuilder();

        stringBuilder.AppendLine("Ваши подписки:");

        var allLocations = GetLocations();

        for (int i = 0; i < subscriptions.Length; ++i)
        {
            var subscription = subscriptions[i];
            var filter = subscription.PerformanceFilter;

            var selectedTheatres = GetTheatres(filter.TheatreIds);

            AddSubscriptionDescription(
                i+1,
                stringBuilder,
                subscription,
                culture,
                allLocations,
                selectedTheatres,
                playbillRepository);

            buttons.Add(new KeyboardButton($"Удалить {i + 1}"));
        }

        stringBuilder.AppendLine(" Что бы удалить несколько подписок напишите текстом Удалить и номера через запятую, например Удалить 1,2,3");

        return Task.FromResult<ITgCommandResponse>(
            new TgCommandResponse($"{stringBuilder}",
                new ReplyKeyboardMarkup(GroupKeyboardButtons(ButtonsInLine, buttons, new[] { new KeyboardButton(DeleteAll), new KeyboardButton(NothingTodo)}))
                {
                    OneTimeKeyboard = true,
                    ResizeKeyboard = true
                }));
    }

    private void AddSubscriptionDescription(int idx, StringBuilder stringBuilder, SubscriptionEntity subscription, CultureInfo culture, LocationsEntity[] locationEntities, string[] theatres, IPlaybillRepository playbillRepository)
    {
        var filter = subscription.PerformanceFilter;

        if (!string.IsNullOrEmpty(filter.Actor))
        {
            stringBuilder.AppendLine($" {idx}. Имя исполнителя: {filter.Actor}");
            return;
        }

        var changesDescription = subscription.TrackingChanges.GetTrackingChangesDescription().ToLower();

        string theatreString = theatres == null || !theatres.Any()
            ? "любой театр"
            : string.Join(" или ", theatres);

        bool anyLocation = filter.LocationIds == null || !filter.LocationIds.Any();

        string locationsString = anyLocation
            ? "все площадки"
            : string.Join(" или ", filter.LocationIds
                    .Where(id => id > 0)
                    .Select(id => locationEntities.First(x => x.Id == id))
                    .Select(GetLocationDescription));

        if (!string.IsNullOrEmpty(filter.PerformanceName))
        {
            stringBuilder.AppendLine($" {idx}. Название содержит \"{filter.PerformanceName}\", место проведения: {locationsString} отслеживаемые события: {changesDescription}");
        }
        else if (filter.PlaybillId == -1)
        {
            string monthName = culture.DateTimeFormat.GetMonthName(filter.StartDate.Month);

            string days = DaysOfWeekHelper.GetDaysDescription(filter.DaysOfWeek, culture);

            string types = filter.PerformanceTypes == null || !filter.PerformanceTypes.Any()
                ? "все представления"
                : string.Join("или ", filter.PerformanceTypes);

            stringBuilder.AppendLine($" {idx}. {monthName} {filter.StartDate.Year}, место проведения: {locationsString}, тип представления: {types}, дни недели: {days} отслеживаемые события: {changesDescription}");
        }
        else
        {
            var playbillEntry = playbillRepository.GetPlaybillEntryWithPerformanceData(filter.PlaybillId);

            if (playbillEntry == null)
                stringBuilder.AppendLine($" {idx}. Подписка на уже прошедший спектакль, отслеживаемые события: {changesDescription}");
            else
            {
                var date = _timeZoneService.GetLocalTime(playbillEntry.When).ToString("ddMMM HH:mm", culture);
                stringBuilder.AppendLine($" {idx}. {playbillEntry.Performance.Name} {date}, отслеживаемые события: {changesDescription}");
            }
        }
    }
}