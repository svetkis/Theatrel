using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
using theatrel.Interfaces.TgBot;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Messages;

namespace theatrel.TLBot.Commands;

internal class SelectTheatreCommand : DialogCommandBase
{
    private const string GoodDay = "Добрый день! ";
    private const string IWillHelpYou = "Я помогу вам подобрать билеты в Мариинский или Михайловский театр. ";
    private const string Msg = "Какой театр вы желаете посетить?";

    private readonly TheatreEntity[] _theatres;
    private readonly string[] _theatreNames;
    private readonly string[] _every = { "Любой", "все", "всё", "любое", "не важно" };

    protected override string ReturnCommandMessage { get; set; } = "Выбрать другой театр";

    public override string Name => "Выбрать театр";

    public SelectTheatreCommand(IDbService dbService) : base(dbService)
    {
        using var repo = dbService.GetPlaybillRepository();
        _theatres = repo.GetTheatres().ToArray();
        _theatreNames = _theatres.Select(x => x.Name).ToArray();

        var buttons = _theatreNames.Select(m => new KeyboardButton(m)).Concat(new[] { new KeyboardButton(_every.First()) })
            .ToArray();

        CommandKeyboardMarkup = new ReplyKeyboardMarkup(GroupKeyboardButtons(ButtonsInLine, buttons))
        {
            OneTimeKeyboard = true,
            ResizeKeyboard = true
        };
    }

    public override Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message,
        CancellationToken cancellationToken)
    {
        chatInfo.TheatreIds = ParseMessage(message);

        var selected = chatInfo.TheatreIds != null && chatInfo.TheatreIds.Any()
            ? string.Join(" или ", chatInfo.TheatreIds.Select(id => _theatres[id-1].Name))
            : "любой театр";

        return Task.FromResult<ITgCommandResponse>(
            new TgCommandResponse($"{YouSelected} {selected}. {ReturnMsg}", ReturnCommandMessage));
    }

    public override bool IsMessageCorrect(IChatDataInfo chatInfo, string message) => SplitMessage(message).Any();

    public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
    {
        return chatInfo.DialogState switch
        {
            DialogState.DialogReturned => Task.FromResult<ITgCommandResponse>(
                new TgCommandResponse(Msg, CommandKeyboardMarkup)),
            DialogState.DialogStarted => Task.FromResult<ITgCommandResponse>(
                new TgCommandResponse($"{GoodDay}{IWillHelpYou}{Msg}", CommandKeyboardMarkup)),
            _ => throw new NotImplementedException()
        };
    }

    private int[] ParseMessage(string message)
    {
        string[] parts = SplitMessage(message).ToArray();

        if (parts.Any(p => int.TryParse(p, out int idx) && idx < _theatres.Length))
        {
            return parts.Where(p => int.TryParse(p, out int idx) && idx < _theatres.Length && idx > 0)
                .Select(p => _theatres[int.Parse(p) - 1].Id).ToArray();
        }

        if (parts.Any(p => _every.Any(e => e.ToLower().Contains(p.ToLower()))))
            return Array.Empty<int>();

        return parts.Select(ParseMessagePart).Where(idx => idx > -1).Select(idx => _theatres[idx].Id).ToArray();
    }

    private static string[] SplitMessage(string message)
        => message.Split(",")
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(s => s.Trim())
            .ToArray();

    private int ParseMessagePart(string messagePart)
    {
        if (string.IsNullOrWhiteSpace(messagePart))
            return -1;

        return CheckEnumerable(_theatreNames, messagePart);
    }

    private static int CheckEnumerable(string[] checkedData, string msg)
    {
        var data = checkedData.Select((item, idx) => new { idx, item })
            .FirstOrDefault(data => 0 == string.Compare(data.item, msg, StringComparison.OrdinalIgnoreCase));

        return data?.idx ?? -1;
    }
}