using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands.SearchByDate;

internal class PerformanceTypesCommand : DialogCommandBase
{
    private readonly string[] _types = { "опера", "балет", "концерт" };
    private readonly string[] _every = { "Все", "всё", "любой", "любое", "не важно" };

    protected override string ReturnCommandMessage { get; set; } = "Выбрать другое";

    public override string Name => "Выбрать тип представления";
    public PerformanceTypesCommand(IDbService dbService) : base(dbService)
    {
        var buttons = new[] { new KeyboardButton(_every.First()) }
        .Concat(_types.Select(m => new KeyboardButton(m)))
        .ToArray();

        CommandKeyboardMarkup = new ReplyKeyboardMarkup(GroupKeyboardButtons(ButtonsInLine, buttons))
        {
            OneTimeKeyboard = true,
            ResizeKeyboard = true
        };
    }

    public override Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
    {
        chatInfo.Types = ParseMessage(message);

        return Task.FromResult<ITgCommandResponse>(new TgCommandResponse(null));
    }

    public override bool IsMessageCorrect(IChatDataInfo chatInfo, string message) => SplitMessage(message).Any();

    public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Какие представления Вас интересуют?");

        return Task.FromResult<ITgCommandResponse>(new TgCommandResponse(stringBuilder.ToString(), CommandKeyboardMarkup));
    }

    private string[] ParseMessage(string message)
    {
        var parts = SplitMessage(message);
        if (parts.Any(p => _every.Any(e => e.ToLower().Contains(p.ToLower()))))
            return Array.Empty<string>();

        return parts.Select(ParseMessagePart).Where(idx => idx > -1).Select(idx => _types[idx]).ToArray();
    }

    private string[] SplitMessage(string message) => message.Split(WordSplitters).Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

    private int ParseMessagePart(string messagePart)
    {
        if (string.IsNullOrWhiteSpace(messagePart))
            return -1;

        return CheckEnumerable(_types, messagePart);
    }

    private int CheckEnumerable(string[] checkedData, string msg)
    {
        var data = checkedData.Select((item, idx) => new { idx, item })
            .FirstOrDefault(data => 0 == string.Compare(data.item, msg, StringComparison.OrdinalIgnoreCase));

        return data?.idx ?? -1;
    }
}