using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
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

        private const string DecreasePriceSubscription = "Подписаться на снижение цены";
        private const string No = "Спасибо, не надо";

        protected override string ReturnCommandMessage { get; set; } = string.Empty;

        public override string Name => "Искать";

        public GetPerformancesCommand(IFilterService filterService, ITimeZoneService timeZoneService, IDbService dbService)
            : base((int)DialogStep.GetPerformances, dbService)
        {
            _filterService = filterService;
            _timeZoneService = timeZoneService;

            CommandKeyboardMarkup = new ReplyKeyboardMarkup
            {
                Keyboard = GroupKeyboardButtons(new[]
                {
                    new KeyboardButton(DecreasePriceSubscription),
                    new KeyboardButton(No),
                }, 1),
                OneTimeKeyboard = true,
                ResizeKeyboard = true
            };
        }

        public override async Task<ITgOutboundMessage> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("GetPerformancesCommand.ApplyResult");

            try
            {
                if (!string.Equals(message, DecreasePriceSubscription, StringComparison.CurrentCultureIgnoreCase))
                    return new TgOutboundMessage("Приятно было пообщаться. Обращайтесь еще.");

                using var subscriptionRepository = DbService.GetSubscriptionRepository();

                SubscriptionEntity subscription = await subscriptionRepository.Create(chatInfo.ChatId,
                    _filterService.GetFilter(chatInfo), cancellationToken);

                return subscription == null
                    ? new TgOutboundMessage("Простите, но я не смог добавить подписку.")
                    : new TgOutboundMessage("Подписка добавлена.");
            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"GetPerformancesCommand.ApplyResult exception : {ex.Message}");
                return new TgOutboundMessage("Простите, но я не смог добавить подписку.");
            }
            finally
            {
                Trace.TraceInformation($"GetPerformancesCommand.ApplyResult finished");
            }
        }

        public override bool IsMessageCorrect(string message)
        {
            return string.Equals(message, DecreasePriceSubscription, StringComparison.InvariantCultureIgnoreCase)
                   || string.Equals(message, No, StringComparison.InvariantCultureIgnoreCase);
        }

        public override async Task<ITgOutboundMessage> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
        {
            IPerformanceFilter filter = _filterService.GetFilter(chatInfo);
            using var playbillRepo = DbService.GetPlaybillRepository();
            PlaybillEntity[] performances = playbillRepo.GetList(filter.StartDate, filter.EndDate).ToArray();
            PlaybillEntity[] filteredPerformances = performances.Where(x => _filterService.IsDataSuitable(x.Performance.Location.Name, x.Performance.Type.TypeName,
                    x.When, filter)).ToArray();

            return new TgOutboundMessage(await PerformancesMessage(filteredPerformances, filter, chatInfo.When, chatInfo.Culture), CommandKeyboardMarkup) {IsEscaped = true};
        }

        private Task<string> PerformancesMessage(PlaybillEntity[] performances, IPerformanceFilter filter, DateTime when, string culture)
        {
            var cultureRu = CultureInfo.CreateSpecificCulture(culture);

            var stringBuilder = new StringBuilder();

            string days = filter.DaysOfWeek != null
                ? filter.DaysOfWeek.Length == 1
                    ? $"день недели: {cultureRu.DateTimeFormat.GetDayName(filter.DaysOfWeek.First())}"
                    : "дни недели: " + string.Join(" или ", filter.DaysOfWeek.Select(d => cultureRu.DateTimeFormat.GetDayName(d)))
                : string.Empty;

            string types = filter.PerformanceTypes == null
                ? "все представления"
                : string.Join(", ", filter.PerformanceTypes);

            stringBuilder.AppendLine(
                $"Я искал для Вас билеты на {when.ToString("MMMM yyyy", cultureRu)} {days} на {types}.".EscapeMessageForMarkupV2());

            foreach (var item in performances.OrderBy(item => item.When).Where(item => item.When > DateTime.Now))
            {
                string minPrice = item.Changes.OrderBy(ch => ch.LastUpdate).Last().MinPrice.ToString();

                DateTime dt = item.When.Kind == DateTimeKind.Utc
                    ? TimeZoneInfo.ConvertTimeFromUtc(item.When, _timeZoneService.TimeZone)
                    : item.When.AddHours(3);

                string performanceString = $"{dt:ddMMM HH:mm} {item.Performance.Location.Name} {item.Performance.Type.TypeName} \"{item.Performance.Name}\" от {minPrice}"
                    .EscapeMessageForMarkupV2();

                stringBuilder.AppendLine(string.IsNullOrWhiteSpace(item.Url)
                    ? performanceString
                    : $"[{performanceString}]({item.Url.EscapeMessageForMarkupV2()})");

                stringBuilder.AppendLine("");
            }

            return Task.FromResult(
                !performances.Any()
                    ? "Увы, я ничего не нашел. Попробуем поискать еще?".EscapeMessageForMarkupV2()
                    : stringBuilder.ToString());
        }
    }
}
