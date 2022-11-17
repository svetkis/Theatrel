using System;
using System.Collections.Generic;
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
        var groupedLocation = GroupLocation(locations);

        chatInfo.LocationIds = ParseMessage(message, groupedLocation);

        var selected = chatInfo.LocationIds != null && chatInfo.LocationIds.Any()
            ? string.Join(" или ", chatInfo.LocationIds.Select(id => locations.First(x => x.Id == id )).Select(x => x.ShortDescription).Distinct())
            : "любую площадку";

        return Task.FromResult<ITgCommandResponse>(
            new TgCommandResponse($"{YouSelected} {selected}. {ReturnMsg}"));
    }

    public override bool IsMessageCorrect(IChatDataInfo chatInfo, string message) => SplitMessage(message).Any();

    private LocationsEntity[] GetLocations(IChatDataInfo chatInfo)
    {
        using var repo = DbService.GetPlaybillRepository();

        bool theatresWereSelected = chatInfo.TheatreIds != null && chatInfo.TheatreIds.Any();
        var theatreIds = theatresWereSelected
            ? chatInfo.TheatreIds
            : repo.GetTheatres().OrderBy(x => x.Id).Select(x => x.Id).ToArray();

        return theatreIds
            .SelectMany(theatreId => repo.GetLocationsList(theatreId)
            .OrderBy(x => x.Id))
            .ToArray();
    }

    private Dictionary<string, List<LocationsEntity>> GroupLocation(LocationsEntity[] locations)
    {
        Dictionary<string, List<LocationsEntity>> groupedLocations = new();

        foreach (var location in locations)
        {
            string button = location.ShortDescription;
            if (string.IsNullOrEmpty(button))
                continue;

            if (!groupedLocations.ContainsKey(button))
                groupedLocations.Add(button, new List<LocationsEntity>());

            groupedLocations[button].Add(location);
        }

        return groupedLocations;
    }

    public override Task<ITgCommandResponse> AscUser(IChatDataInfo chatInfo, CancellationToken cancellationToken)
    {
        CommandKeyboardMarkup = new ReplyKeyboardMarkup(GetKeyboardButtons(chatInfo))
        {
            OneTimeKeyboard = true,
            ResizeKeyboard = true
        };

        return Task.FromResult<ITgCommandResponse>(new TgCommandResponse(Msg, CommandKeyboardMarkup));
    }

    private KeyboardButton[][] GetKeyboardButtons(IChatDataInfo chatInfo)
    {
        List<KeyboardButton[]> groupedButtons = new();

        var buttonNames = GetLocations(chatInfo)
            .Select(x => x.ShortDescription)
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        groupedButtons.Add(new KeyboardButton[] { new KeyboardButton(_every.First()) });
        foreach (KeyboardButton[] line in GroupKeyboardButtons(ButtonsInLine, buttonNames.Select(m => new KeyboardButton(m))))
        {
            groupedButtons.Add(line);
        }

        return groupedButtons.ToArray();
    }

    private int[] ParseMessage(string message, Dictionary<string, List<LocationsEntity>> groupedLocation)
    {
        string[] parts = SplitMessage(message).ToArray();

        var buttons = groupedLocation.Keys.OrderBy(x => x).ToArray();

        if (parts.Any(p => int.TryParse(p, out int idx) && idx < groupedLocation.Count))
        {
            return parts
                .Where(p => int.TryParse(p, out int idx) && idx < groupedLocation.Count && idx > 0)
                .SelectMany(p => groupedLocation[buttons[int.Parse(p) - 1]].Select(x => x.Id))
                .ToArray();
        }

        if (parts.Any(p => _every.Any(e => e.ToLower().Contains(p.ToLower()))))
            return groupedLocation.SelectMany(x => x.Value.Select(s => s.Id)).ToArray();

        return parts
            .Where(x => buttons.Contains(x))
            .SelectMany(x => groupedLocation[x].Select(x => x.Id))
            .ToArray();
    }

    private static string[] SplitMessage(string message)
        => message.Split(",")
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(s => s.Trim())
            .ToArray();

    private static int CheckEnumerable(string[] checkedData, string msg)
    {
        var data = checkedData.Select((item, idx) => new { idx, item })
            .FirstOrDefault(data => 0 == string.Compare(data.item, msg, StringComparison.OrdinalIgnoreCase));

        return data?.idx ?? -1;
    }
}