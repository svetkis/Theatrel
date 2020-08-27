using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Playbill;
using theatrel.Interfaces.TgBot;
using theatrel.Interfaces.TimeZoneService;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands
{
    internal class GetPerformancesCommand : DialogCommandBase
    {
        private readonly IPlayBillDataResolver _playBillResolver;
        private readonly IFilterService _filterService;
        private readonly ITimeZoneService _timeZoneService;

        protected override string ReturnCommandMessage { get; set; } = string.Empty;

        public override string Name => "Искать";

        public GetPerformancesCommand(IPlayBillDataResolver playBillResolver, IFilterService filterService, ITimeZoneService timeZoneService) : base((int)DialogStep.Final)
        {
            _playBillResolver = playBillResolver;
            _filterService = filterService;
            _timeZoneService = timeZoneService;
        }

        public override Task<ITgOutboundMessage> ApplyResultAsync(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
            => Task.FromResult<ITgOutboundMessage>(new TgOutboundMessage(null));

        public override bool IsMessageCorrect(string message) => true;

        public override async Task<ITgOutboundMessage> AscUserAsync(IChatDataInfo chatInfo, CancellationToken cancellationToken)
        {
            IPerformanceFilter filter = _filterService.GetFilter(chatInfo);

            IPerformanceData[] data = await _playBillResolver.RequestProcess(filter, cancellationToken);

            return new TgOutboundMessage(await PerformancesMessage(data, filter, chatInfo.When))
            {
                IsEscaped = true
            };
        }

        private Task<string> PerformancesMessage(IPerformanceData[] performances, IPerformanceFilter filter, DateTime when)
        {
            var cultureRu = CultureInfo.CreateSpecificCulture("ru");

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

            foreach (var item in performances.OrderBy(item => item.DateTime))
            {
                string minPrice = item.MinPrice.ToString();

                DateTime dt = item.DateTime.Kind == DateTimeKind.Utc
                    ? TimeZoneInfo.ConvertTimeFromUtc(item.DateTime, _timeZoneService.TimeZone)
                    : item.DateTime;

                string performanceString = $"{dt:ddMMM HH:mm} {item.Location} {item.Type} \"{item.Name}\" от {minPrice}"
                    .EscapeMessageForMarkupV2();

                stringBuilder.AppendLine(string.IsNullOrWhiteSpace(item.Url)
                    ? performanceString
                    : $"[{performanceString}]({item.Url.EscapeMessageForMarkupV2()})");

                stringBuilder.AppendLine("");
            }

            if (!performances.Any())
                return Task.FromResult("Увы, я ничего не нашел. Попробуем поискать еще?".EscapeMessageForMarkupV2());

            return Task.FromResult(stringBuilder.ToString());
        }
    }
}
