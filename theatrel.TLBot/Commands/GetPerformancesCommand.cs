using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.Common;
using theatrel.Common.Enums;
using theatrel.Common.FormatHelper;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.TgBot;
using theatrel.Interfaces.TimeZoneService;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands
{
    internal class GetPerformancesCommand : DialogCommandBase
    {
        private readonly IFilterService _filterService;
        private readonly ITimeZoneService _timeZoneService;

        private const string DecreasePriceSubscription = "Подписаться на снижение цены на билеты";
        private const string NewInPlaybillSubscription = "Подписаться на новые спектакли и появление билетов в продаже";
        private const string CastSubscription = "Подписаться на изменения в составе исполнителей";

        private const string No = "Спасибо, не надо";

        protected override string ReturnCommandMessage { get; set; } = string.Empty;

        public override string Name => "Искать";

        public GetPerformancesCommand(IFilterService filterService, ITimeZoneService timeZoneService, IDbService dbService)
            : base(dbService)
        {
            _filterService = filterService;
            _timeZoneService = timeZoneService;

            CommandKeyboardMarkup = new ReplyKeyboardMarkup
            {
                Keyboard = GroupKeyboardButtons(1, new[]
                {
                    new KeyboardButton(DecreasePriceSubscription),
                    new KeyboardButton(No),
                }),
                OneTimeKeyboard = true,
                ResizeKeyboard = true
            };
        }

        public override async Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("GetPerformancesCommand.ApplyResult");

            int trackingChanges;

            switch (message)
            {
                case DecreasePriceSubscription:
                    trackingChanges = (int) ReasonOfChanges.PriceDecreased;
                    break;
                case NewInPlaybillSubscription:
                    trackingChanges = (int)(ReasonOfChanges.StartSales | ReasonOfChanges.Creation);
                    break;

                case CastSubscription:
                    trackingChanges = (int)(ReasonOfChanges.CastWasSet | ReasonOfChanges.CastWasChanged);
                    break;

                default:
                    return new TgCommandResponse("Приятно было пообщаться. Обращайтесь еще.");
            }

            using var subscriptionRepository = DbService.GetSubscriptionRepository();

            SubscriptionEntity subscription = await subscriptionRepository.Create(chatInfo.UserId, trackingChanges,
                _filterService.GetFilter(chatInfo), cancellationToken);

            return subscription == null
                ? new TgCommandResponse("Простите, но я не смог добавить подписку.")
                : new TgCommandResponse("Подписка добавлена.");
        }

        public override bool IsMessageCorrect(string message) => true;

        public override async Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
        {
            IPerformanceFilter filter = _filterService.GetFilter(chatInfo);

            using var playbillRepo = DbService.GetPlaybillRepository();

            PlaybillEntity[] performances = !string.IsNullOrEmpty(chatInfo.PerformanceName)
                ? playbillRepo.GetListByName(chatInfo.PerformanceName).ToArray()
                : playbillRepo.GetList(filter.StartDate, filter.EndDate).ToArray();

            PlaybillEntity[] filteredPerformances = performances.Where(x =>
            {
                if (!_filterService.IsDataSuitable(x.Performance.Name, x.Performance.Location.Name,
                    x.Performance.Type.TypeName,
                    x.When, filter))
                    return false;

                if (!x.Changes.Any())
                    return true;

                var lastChange = x.Changes.OrderBy(ch => ch.LastUpdate).Last();
                return lastChange.ReasonOfChanges != (int) ReasonOfChanges.WasMoved;
            }).ToArray();

            List<KeyboardButton> buttons = new List<KeyboardButton> { new KeyboardButton(NewInPlaybillSubscription) };
            if (filteredPerformances.Any())
            {
                buttons.Add(new KeyboardButton(DecreasePriceSubscription));
                buttons.Add(new KeyboardButton(CastSubscription));
            }

            buttons.Add(new KeyboardButton(No));

            var keys = new ReplyKeyboardMarkup
            {
                Keyboard = GroupKeyboardButtons(1, buttons),
                OneTimeKeyboard = true,
                ResizeKeyboard = true
            };

            return new TgCommandResponse(await PerformancesMessage(filteredPerformances, filter, chatInfo.When, chatInfo.Culture), keys) { IsEscaped = true };
        }

        private Task<string> PerformancesMessage(PlaybillEntity[] performances, IPerformanceFilter filter, DateTime when, string culture)
        {
            var cultureRu = CultureInfo.CreateSpecificCulture(culture);

            var stringBuilder = new StringBuilder();

            string days = filter.DaysOfWeek != null
                ? filter.DaysOfWeek.Length == 1
                    ? $"день недели: {cultureRu.DateTimeFormat.GetDayName(filter.DaysOfWeek.First())}"
                    : "дни недели: " + string.Join(" или ", filter.DaysOfWeek
                            .OrderBy(d => (int)d, DaysOfWeekComparer.Create())
                            .Select(d => cultureRu.DateTimeFormat.GetDayName(d)))
                : string.Empty;

            string types = filter.PerformanceTypes == null
                ? "все представления"
                : string.Join(", ", filter.PerformanceTypes);

            string locations = filter.Locations == null
                ? "любая площадка"
                : string.Join(", ", filter.Locations);

            stringBuilder.AppendLine(
                string.IsNullOrEmpty(filter.PerformanceName)
                    ? $"Я искал для Вас билеты на {when.ToString("MMMM yyyy", cultureRu)} {days} на {types} площадка: {locations}.".EscapeMessageForMarkupV2()
                    : $"Я искал для Вас билеты на \"{filter.PerformanceName}\" площадка: {locations}".EscapeMessageForMarkupV2());

            stringBuilder.AppendLine();

            foreach (var item in performances.OrderBy(item => item.When).Where(item => item.When > DateTime.Now))
            {
                if (!item.Changes.Any())
                    continue;

                var lastChange = item.Changes.OrderBy(ch => ch.LastUpdate).Last();
                if (lastChange.ReasonOfChanges == (int)ReasonOfChanges.WasMoved)
                    continue;

                int minPrice = lastChange.MinPrice;

                DateTime dt = item.When.Kind == DateTimeKind.Utc
                    ? TimeZoneInfo.ConvertTimeFromUtc(item.When, _timeZoneService.TimeZone)
                    : item.When.AddHours(3);

                string firstPart = $"{dt.ToString("ddMMM HH:mm", cultureRu)} {item.Performance.Location.Name} {item.Performance.Type.TypeName}"
                    .EscapeMessageForMarkupV2();

                string escapedName = $"\"{item.Performance.Name}\"".EscapeMessageForMarkupV2();
                string performanceString = string.IsNullOrWhiteSpace(item.Url) || item.Url == CommonTags.NotDefinedTag
                    ? escapedName
                    : $"[{escapedName}]({item.Url.EscapeMessageForMarkupV2()})";

                string lastPart = minPrice > 0
                    ? string.IsNullOrWhiteSpace(item.TicketsUrl) || item.TicketsUrl == CommonTags.NotDefinedTag
                        ? $"от {minPrice}"
                        : $"от [{minPrice}]({item.TicketsUrl.EscapeMessageForMarkupV2()})"
                    : "Нет билетов в продаже";

                stringBuilder.AppendLine($"{firstPart} {performanceString} {lastPart}");
                stringBuilder.AppendLine();
            }

            return Task.FromResult(
                !performances.Any()
                    ? "Увы, я ничего не нашел. Попробуем поискать еще?".EscapeMessageForMarkupV2()
                    : stringBuilder.ToString());
        }
    }
}
