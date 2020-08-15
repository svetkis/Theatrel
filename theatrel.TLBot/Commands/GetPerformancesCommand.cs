using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;
using IFilterHelper = theatrel.Interfaces.IFilterHelper;

namespace theatrel.TLBot.Commands
{
    internal class GetPerformancesCommand : DialogCommandBase
    {
        private readonly IPlayBillDataResolver _playBillResolver;
        private readonly IFilterHelper _filterHelper;

        protected override string ReturnCommandMessage { get; set; } = string.Empty;

        public override string Name => "Искать";

        public GetPerformancesCommand(IPlayBillDataResolver playBillResolver, IFilterHelper filterHelper) : base ((int)DialogStep.Final)
        {
            _playBillResolver = playBillResolver;
            _filterHelper = filterHelper;
        }

        public override async Task<ITlOutboundMessage> ApplyResultAsync(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
            => new TlOutboundMessage(null);

        public override bool IsMessageCorrect(string message) => true;

        public override async Task<ITlOutboundMessage> AscUserAsync(IChatDataInfo chatInfo, CancellationToken cancellationToken)
        {
            IPerformanceFilter filter = _filterHelper.GetFilter(chatInfo);

            IPerformanceData[] data = await _playBillResolver.RequestProcess(filter, cancellationToken);

            return new TlOutboundMessage(await PerformancesMessage(data, filter, chatInfo.When))
            {
                IsEscaped = true
            };
        }

        private async Task<string> PerformancesMessage(IPerformanceData[] performances, IPerformanceFilter filter, DateTime when)
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

                string performanceString = $"{item.DateTime:ddMMM HH:mm} {item.Location} {item.Type} \"{item.Name}\" от {minPrice}"
                    .EscapeMessageForMarkupV2();

                stringBuilder.AppendLine(string.IsNullOrWhiteSpace(item.Url)
                    ? performanceString
                    : $"[{performanceString}]({item.Url.EscapeMessageForMarkupV2()})");

                stringBuilder.AppendLine("");
            }

            if (!performances.Any())
                return "Увы, я ничего не нашел. Попробуем поискать еще?".EscapeMessageForMarkupV2();

            return stringBuilder.ToString();
        }
    }
}
