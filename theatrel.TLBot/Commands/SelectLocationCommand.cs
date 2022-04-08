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

internal class SelectLocationCommand : DialogCommandBase
{
    private const string Msg = "Какую площадку вы желаете посетить?";

    private readonly string[] _every = { "Любую", "все", "всё", "любой", "любое", "не важно" };

    protected override string ReturnCommandMessage { get; set; } = "Выбрать другую площадку";

    public override string Name => "Выбрать площадку";
    public SelectLocationCommand(IDbService dbService) : base(dbService)
    {
    }

    public override Task<ITgCommandResponse> ApplyResult(IChatDataInfo chatInfo, string message, CancellationToken cancellationToken)
    {
        LocationsEntity[] locations = GetLocations(chatInfo);

        chatInfo.LocationIds = ParseMessage(message, locations);

        var selected = chatInfo.LocationIds != null && chatInfo.LocationIds.Any() ? string.Join(" или ", chatInfo.LocationIds.Select(id => locations.First(x => x.Id == id )).Select(GetLocationButtonName)) : "любую площадку";
        return Task.FromResult<ITgCommandResponse>(
            new TgCommandResponse($"{YouSelected} {selected}. {ReturnMsg}", ReturnCommandMessage));
    }

    public override bool IsMessageCorrect(IChatDataInfo chatInfo, string message) => SplitMessage(message).Any();

    private LocationsEntity[] GetLocations(IChatDataInfo chatInfo)
    {
        using var repo = DbService.GetPlaybillRepository();

        bool theatresWereSelected = chatInfo.TheatreIds != null && chatInfo.TheatreIds.Any();
        var theatreIds = theatresWereSelected ? chatInfo.TheatreIds : repo.GetTheatres().OrderBy(x => x.Id).Select(x => x.Id).ToArray();

        return theatreIds.SelectMany(theatreId => repo.GetLocationsList(theatreId).OrderBy(x => x.Id)).ToArray();
    }

    private string[] GetLocationButtonNames(LocationsEntity[] locations) => locations.Select(GetLocationButtonName).ToArray();

    private string GetLocationButtonName(LocationsEntity location) => string.IsNullOrEmpty(location.Description) ? location.Name : location.Description;

    public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
    {
        var locations = GetLocations(chatInfo);
        var buttonNames = GetLocationButtonNames(locations);

        var buttons = buttonNames.Select(m => new KeyboardButton(m)).Concat(new[] { new KeyboardButton(_every.First()) }).ToArray();

        CommandKeyboardMarkup = new ReplyKeyboardMarkup(GroupKeyboardButtons(ButtonsInLine, buttons))
        {
            OneTimeKeyboard = true,
            ResizeKeyboard = true
        };

        return chatInfo.DialogState switch
        {
            DialogState.DialogReturned => Task.FromResult<ITgCommandResponse>(
                new TgCommandResponse(Msg, CommandKeyboardMarkup)),
            DialogState.DialogStarted => Task.FromResult<ITgCommandResponse>(
                new TgCommandResponse($"{Msg}", CommandKeyboardMarkup)),
            _ => throw new NotImplementedException()
        };
    }

    private int[] ParseMessage(string message, LocationsEntity[] locations)
    {
        string[] parts = SplitMessage(message).ToArray();

        if (parts.Any(p => int.TryParse(p, out int idx) && idx < locations.Length))
        {
            return parts.Where(p => int.TryParse(p, out int idx) && idx < locations.Length && idx > 0)
                .Select(p => locations[int.Parse(p) - 1].Id).ToArray();
        }

        if (parts.Any(p => _every.Any(e => e.ToLower().Contains(p.ToLower()))))
            return Array.Empty<int>();

        var buttonNames = GetLocationButtonNames(locations);
        return parts.Select(x => ParseMessagePart(x, buttonNames)).Where(idx => idx > -1).Select(idx => locations[idx].Id).ToArray();
    }

    private static string[] SplitMessage(string message)
        => message.Split(",")
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(s => s.Trim())
            .ToArray();

    private int ParseMessagePart(string messagePart, string[] locations)
    {
        if (string.IsNullOrWhiteSpace(messagePart))
            return -1;

        return CheckEnumerable(locations, messagePart);
    }

    private static int CheckEnumerable(string[] checkedData, string msg)
    {
        var data = checkedData.Select((item, idx) => new { idx, item })
            .FirstOrDefault(data => 0 == string.Compare(data.item, msg, StringComparison.OrdinalIgnoreCase));

        return data?.idx ?? -1;
    }
}