using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands.SearchByDate;

internal class MonthCommand : DialogCommandBase
{
    private const string Msg = "Какой месяц Вас интересует?";
    private readonly string[] _monthNames;
    private readonly string[] _monthNamesAbbreviated;

    protected override string ReturnCommandMessage { get; set; } = "Выбрать другой месяц";

    public override string Name => "Выбрать месяц";

    public MonthCommand(IDbService dbService) : base(dbService)
    {
        var cultureRu = CultureInfo.CreateSpecificCulture("ru");

        _monthNames = Enumerable.Range(1, 12).Select(num => cultureRu.DateTimeFormat.GetMonthName(num)).ToArray();
        _monthNamesAbbreviated = Enumerable.Range(1, 12).Select(num => cultureRu.DateTimeFormat.GetAbbreviatedMonthName(num)).ToArray();

        var buttons = _monthNames.Select(m => new KeyboardButton(m)).ToArray();
        CommandKeyboardMarkup = new ReplyKeyboardMarkup(GroupKeyboardButtons(ButtonsInLine, buttons))
        {
            OneTimeKeyboard = true,
            ResizeKeyboard = true
        };
    }

    public override bool IsMessageCorrect(IChatDataInfo chatInfo, string message) => 0 != GetMonth(message.Trim().ToLower());

    private int GetMonth(string msg)
    {
        if (int.TryParse(msg, out var value))
        {
            if (value > 0 && value < 12)
                return value;

            return 0;
        }

        int num = CheckEnumerable(_monthNames, msg);
        if (num != 0)
            return num;

        int numAbr = CheckEnumerable(_monthNamesAbbreviated, msg);

        return numAbr;
    }

    public override Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
    {
        int month = GetMonth(message.Trim().ToLower());

        int year = DateTime.UtcNow.Month > month ? DateTime.UtcNow.Year + 1 : DateTime.UtcNow.Year;

        chatInfo.When = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);

        var culture = CultureInfo.CreateSpecificCulture(chatInfo.Culture);

        var userMsg = $"{YouSelected} {culture.DateTimeFormat.GetMonthName(month)} {year}. {ReturnMsg}";

        return Task.FromResult<ITgCommandResponse>(new TgCommandResponse(userMsg, ReturnKeyboardMarkup));
    }

    public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
    {
        return Task.FromResult<ITgCommandResponse>(new TgCommandResponse(Msg, CommandKeyboardMarkup));
    }

    private int CheckEnumerable(string[] checkedData, string msg)
    {
        var monthData = checkedData.Select((month, idx) => new { idx, month })
            .FirstOrDefault(data => msg.Equals(data.month, StringComparison.OrdinalIgnoreCase));

        return monthData?.idx + 1 ?? 0;
    }
}