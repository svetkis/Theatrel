using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces;
using theatrel.TLBot.Interfaces;

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

        public override async Task<ICommandResponse> ApplyResultAsync(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
            => new TlCommandResponse(null);

        public override bool IsMessageCorrect(string message) => true;

        public override async Task<ICommandResponse> AscUserAsync(IChatDataInfo chatInfo, CancellationToken cancellationToken)
        {
            IPerformanceFilter filter = _filterHelper.GetFilter(chatInfo);

            IPerformanceData[] data = await _playBillResolver.RequestProcess(chatInfo.When, new DateTime(), filter, cancellationToken);

            return new TlCommandResponse(await PerformancesMessage(data, filter, chatInfo.When));
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

            stringBuilder.AppendLine($"Я искал для Вас билеты на {when.ToString("MMMM yyyy", cultureRu)} {days} на {types}.");
            foreach (var item in performances.OrderBy(item => item.DateTime))
            {
                string minPrice = item.Tickets.GetMinPrice().ToString();

                stringBuilder.AppendLine(string.IsNullOrWhiteSpace(item.Url)
                    ? $"{item.DateTime:ddMMM HH:mm} {item.Location} {item.Type} \"{item.Name}\" {minPrice}"
                    : $"[{item.DateTime:ddMMM HH:mm} {item.Location} {item.Type} \"{item.Name}\" от {minPrice}]({item.Url})");

                stringBuilder.AppendLine("");
            }

            if (!performances.Any())
                return "Увы, я ничего не нашел. Попробуем поискать еще?";

            return stringBuilder.ToString();
        }
    }
}
