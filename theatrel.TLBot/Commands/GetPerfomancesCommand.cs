using System;
using System.Diagnostics;
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
            var filter = _filterhelper.GetFilter(chatInfo);

            IPerformanceData[] data = await _playBillResolver.RequestProcess(chatInfo.When, new DateTime(), filter);

            return await PerfomancesMessage(data);
        }

        private async Task<string> PerfomancesMessage(IPerformanceData[] perfomances)
        {
            var stringBuilder = new StringBuilder();
            foreach (var item in perfomances.OrderBy(item => item.DateTime))
            {
                if (item.Tickets.GetMinPrice() == 0)
                    continue;

                string minPrice = item.Tickets.GetMinPrice().ToString() ?? item.Tickets.Description;
                stringBuilder.AppendLine($"[{item.DateTime:ddMMM HH:mm} {item.Location} {item.Type} \"{item.Name}\" от {minPrice}]({item.Url})");
                stringBuilder.AppendLine("");
            }

            if (string.IsNullOrWhiteSpace(stringBuilder.ToString()))
                return "Увы, я не нашел билетов на интересующие вас даты.";

            return stringBuilder.ToString();
        }
    }
}
