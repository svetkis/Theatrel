using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot.Commands
{
    internal class MonthCommand : DialogCommandBase
    {

        private string[] _monthNames;
        private string[] _monthNamesAbbreviated;

        public MonthCommand() : base((int)DialogStep.SelectMonth)
        {
            var cultureRu = CultureInfo.CreateSpecificCulture("ru");

            _monthNames = Enumerable.Range(1, 12).Select(num => cultureRu.DateTimeFormat.GetMonthName(num)).ToArray();
            _monthNamesAbbreviated = Enumerable.Range(1, 12).Select(num => cultureRu.DateTimeFormat.GetAbbreviatedMonthName(num)).ToArray();
        }

        public override bool IsMessageClear(string message) => 0 != GetMonth(message.Trim().ToLower());

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

        public override string ApplyResult(IChatDataInfo chatInfo, string message)
        {
            int month = GetMonth(message.Trim().ToLower());

            int year = DateTime.Now.Month > month ? DateTime.Now.Year + 1 : DateTime.Now.Year;

            chatInfo.When = new DateTime(year, month, 1);

            var culture = CultureInfo.CreateSpecificCulture(chatInfo.Culture);
            return $"Вы выбрали {culture.DateTimeFormat.GetMonthName(month)} {year}. Для того что бы выбрать другое напишите Нет.";
        }

        public override async Task<string> ExecuteAsync(IChatDataInfo chatInfo)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Добрый день! Я помогу вам подобрать билеты в Мариинский театр. Какой месяц вас интересует?");
            return stringBuilder.ToString();
        }

        private int CheckEnumarable(string[] checkedData, string msg)
        {
            var monthData = checkedData.Select((month, idx) => new { idx, month })
                .FirstOrDefault(data => msg.Equals(data.month, StringComparison.OrdinalIgnoreCase ));

            return null != monthData ? monthData.idx + 1 : 0;
        }
    }
}
