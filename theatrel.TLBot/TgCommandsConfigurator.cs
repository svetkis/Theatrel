﻿using theatrel.DataAccess.DbService;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.TimeZoneService;
using theatrel.TLBot.Commands.SearchPerformances;
using theatrel.TLBot.Commands.Subscriptions;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot
{
    internal class TgCommandsConfigurator: ITgCommandsConfigurator
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
                    new StartSearchCommand(_dbService),
                    new MonthCommand(_dbService),
                    new DaysOfWeekCommand(_dbService),
                    new PerformanceTypesCommand(_dbService),
                    new GetPerformancesCommand(_filterService, _timeZoneService, _dbService)
                },
                new IDialogCommand[]
                {
                    new StartSubscriptionsManagingCommand(_dbService),
                    new ManageSubscriptionsCommand(_dbService),
                }
            };
        }
    }
}
