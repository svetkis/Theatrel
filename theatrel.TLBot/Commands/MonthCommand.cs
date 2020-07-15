using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Commands
{
    internal class MonthCommand : DialogCommandBase
    {
        private string GoodDay = "Добрый день! ";
        private string IWillHelpYou = "Я помогу вам подобрать билеты в Мариинский театр. ";
        private string Msg = "Какой месяц вас интересует?";

        private readonly string[] _monthNames;
        private readonly string[] _monthNamesAbbreviated;

        public MonthCommand() : base((int)DialogStep.SelectMonth)
        {
            var cultureRu = CultureInfo.CreateSpecificCulture("ru");

            _monthNames = Enumerable.Range(1, 12).Select(num => cultureRu.DateTimeFormat.GetMonthName(num)).ToArray();
            _monthNamesAbbreviated = Enumerable.Range(1, 12).Select(num => cultureRu.DateTimeFormat.GetAbbreviatedMonthName(num)).ToArray();
        }

        public override bool IsMessageReturnToStart(string message) => 0 != GetMonth(message.Trim().ToLower());

        private int GetMonth(string msg)
        {
            int value;
            if (int.TryParse(msg, out value))
            {
                if (value > 0 && value < 12)
                    return value;

                return 0;
            }

            int num = CheckEnumarable(_monthNames, msg);
            if (num != 0)
                return num;

            int numAbr = CheckEnumarable(_monthNamesAbbreviated, msg);
            if (numAbr != 0)
                return numAbr;

            return 0;
        }

        public override async Task<string> ApplyResultAsync(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
        {
            int month = GetMonth(message.Trim().ToLower());

            int year = DateTime.Now.Month > month ? DateTime.Now.Year + 1 : DateTime.Now.Year;

            chatInfo.When = new DateTime(year, month, 1);

            var culture = CultureInfo.CreateSpecificCulture(chatInfo.Culture);
            return $"Вы выбрали {culture.DateTimeFormat.GetMonthName(month)} {year}. {ReturnMsg}";
        }

        public override async Task<string> ExecuteAsync(IChatDataInfo chatInfo, CancellationToken cancellationToken)
        {
            switch (chatInfo.DialogState)
            {
                case DialogStateEnum.DialogReturned:
                    return Msg;
                case DialogStateEnum.DialogStarted:
                    return $"{GoodDay}{IWillHelpYou}{Msg}";
                default:
                    throw new NotImplementedException();
            }
        }

        private int CheckEnumarable(string[] checkedData, string msg)
        {
            var monthData = checkedData.Select((month, idx) => new { idx, month })
                .FirstOrDefault(data => msg.Equals(data.month, StringComparison.OrdinalIgnoreCase ));

            return null != monthData ? monthData.idx + 1 : 0;
        }
    }
}
