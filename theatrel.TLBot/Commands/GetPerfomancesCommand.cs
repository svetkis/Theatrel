using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using theatrel.Interfaces;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Commands
{
    internal class GetPerfomancesCommand : DialogCommandBase
    {
        private readonly IPlayBillDataResolver _playBillResolver;
        private readonly IFilterHelper _filterhelper;

        public GetPerfomancesCommand(IPlayBillDataResolver playBillResolver, IFilterHelper filterhelper) : base ((int)DialogStep.Final)
        {
            _playBillResolver = playBillResolver;
            _filterhelper = filterhelper;
        }

        public override void ApplyResult(IChatDataInfo chatInfo, string message)
        {
        }

        public override bool CanExecute(string message) => true;

        public override async Task<string> ExecuteAsync(IChatDataInfo chatInfo)
        {
            IPerformanceFilter filter = _filterhelper.GetFilter(chatInfo);

            IPerformanceData[] data = await _playBillResolver.RequestProcess(chatInfo.When, new DateTime(), filter);

            return await PerfomancesMessage(data, filter, chatInfo.When);
        }

        private async Task<string> PerfomancesMessage(IPerformanceData[] perfomances, IPerformanceFilter filter, DateTime when)
        {
            var cultureRu = CultureInfo.CreateSpecificCulture("ru");

            var stringBuilder = new StringBuilder();

            string days = filter.DaysOfWeek != null
                ? string.Join(" ", filter.DaysOfWeek.Select(d => cultureRu.DateTimeFormat.GetDayName(d)))
                : string.Empty;

            string types = filter.PerfomanceTypes == null
                ? "все представления"
                : string.Join(", ", filter.PerfomanceTypes);

            stringBuilder.AppendLine($"Мы искали билеты {when:MMM YY} дни недели: {days} на {types}");
            foreach (var item in perfomances.OrderBy(item => item.DateTime))
            {
                string minPrice = item.Tickets.GetMinPrice().ToString() ?? item.Tickets.Description;
                if (string.IsNullOrWhiteSpace(item.Url))
                    stringBuilder.AppendLine($"{item.DateTime:ddMMM HH:mm} {item.Location} {item.Type} \"{item.Name}\" {minPrice}");
                else
                    stringBuilder.AppendLine($"[{item.DateTime:ddMMM HH:mm} {item.Location} {item.Type} \"{item.Name}\" от {minPrice}]({item.Url})");

                stringBuilder.AppendLine("");
            }

            if (string.IsNullOrWhiteSpace(stringBuilder.ToString()))
                return "Увы, я не нашел событий на интересующие вас даты.";

            return stringBuilder.ToString();
        }
    }
}
