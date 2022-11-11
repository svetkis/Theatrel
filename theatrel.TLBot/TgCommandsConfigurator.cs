using theatrel.DataAccess.DbService;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.TimeZoneService;
using theatrel.TLBot.Commands;
using theatrel.TLBot.Commands.IntroduceBot;
using theatrel.TLBot.Commands.SearchByActor;
using theatrel.TLBot.Commands.SearchByDate;
using theatrel.TLBot.Commands.SearchByName;
using theatrel.TLBot.Commands.Subscriptions;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot;

internal class TgCommandsConfigurator : ITgCommandsConfigurator
{
    private readonly IDbService _dbService;
    private readonly IFilterService _filterService;
    private readonly ITimeZoneService _timeZoneService;

    public TgCommandsConfigurator(IDbService dbService, IFilterService filterService, ITimeZoneService timeZoneService)
    {
        _dbService = dbService;
        _filterService = filterService;
        _timeZoneService = timeZoneService;
    }

    public IDialogCommand[][] GetDialogCommands()
    {
        return new[]
        {
            new IDialogCommand[]
            {
                new IntroduceStart(_dbService),
                new IntroduceMyself(_dbService),
            },
            new IDialogCommand[]
            {
                new StartSearchByDateCommand(_dbService),
                new SelectTheatreCommand(_dbService),
                new SelectLocationCommand(_dbService),
                new MonthCommand(_dbService),
                new DaysOfWeekCommand(_dbService),
                new PerformanceTypesCommand(_dbService),
                new GetPerformancesCommand(_filterService, _timeZoneService, _dbService)
            },
            new IDialogCommand[]
            {
                new StartSearchByNameCommand(_dbService),
                new SelectTheatreCommand(_dbService),
                new SelectLocationCommand(_dbService),
                new AscNameCommand(_dbService),
                new GetPerformancesCommand(_filterService, _timeZoneService, _dbService)
            },
            new IDialogCommand[]
            {
                new StartSearchByActorCommand(_dbService),
                new AcsActorCommand(_dbService),
                new GetPerformancesByActorCommand(_filterService, _timeZoneService, _dbService)
            },
            new IDialogCommand[]
            {
                new StartSubscriptionsManagingCommand(_dbService),
                new ManageSubscriptionsCommand(_dbService, _timeZoneService),
            }
        };
    }
}