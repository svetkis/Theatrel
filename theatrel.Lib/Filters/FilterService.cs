using System;
using System.Linq;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Playbill;
using theatrel.Interfaces.TgBot;

namespace theatrel.Lib.Filters;

internal class FilterService : IFilterService
{
    public IPerformanceFilter GetFilter(IChatDataInfo dataInfo)
    {
        if (!string.IsNullOrEmpty(dataInfo.PerformanceName))
        {
            return new PerformanceFilter
            {
                PerformanceName = dataInfo.PerformanceName,
                Locations = dataInfo.Locations
            };
        }

        var filter = new PerformanceFilter
        {
            StartDate = dataInfo.When,
            EndDate = dataInfo.When.AddMonths(1)
        };

        if (dataInfo.Theatres != null && dataInfo.Theatres.Any())
            filter.Theatres = dataInfo.Theatres;

        if (dataInfo.Locations != null && dataInfo.Locations.Any())
            filter.Locations = dataInfo.Locations;

        if (dataInfo.Days != null && dataInfo.Days.Any())
        {
            var days = dataInfo.Days.Distinct().ToArray();
            if (days.Length < 7)
                filter.DaysOfWeek = days.ToArray();
        }

        if (dataInfo.Types != null && dataInfo.Types.Any())
            filter.PerformanceTypes = dataInfo.Types;

        return filter;
    }

    public IPerformanceFilter GetFilter(DateTime start, DateTime end) =>
        new PerformanceFilter
        {
            StartDate = start,
            EndDate = end
        };

    public IPerformanceFilter GetFilter(int playbillEntryId) =>
        new PerformanceFilter
        {
            PlaybillId = playbillEntryId
        };

    public bool IsDataSuitable(IPerformanceData performance, IPerformanceFilter filter) =>
        IsDataSuitable(-1,performance.Name, performance.Location, performance.Type, performance.DateTime, filter);

    private static bool CheckLocation(string[] filterLocations, string location)
        => filterLocations == null || !filterLocations.Any() ||
           filterLocations.Select(l => l.ToLower()).Contains(location.ToLower());

    public bool IsDataSuitable(int playbillEntryId, string name, string location, string type, DateTime when, IPerformanceFilter filter)
    {
        if (filter == null)
            return true;

        if (filter.PlaybillId != -1 && playbillEntryId != -1)
            return filter.PlaybillId == playbillEntryId;

        if (!string.IsNullOrEmpty(filter.PerformanceName))
            return name.ToLower().Contains(filter.PerformanceName.ToLower()) && CheckLocation(filter.Locations, location);

        if (!CheckLocation(filter.Locations, location))
            return false;

        if (filter.PerformanceTypes != null && filter.PerformanceTypes.Any() && !string.IsNullOrEmpty(filter.PerformanceTypes.First())
            && filter.PerformanceTypes.All(val => 0 != string.Compare(val, type, true)))
            return false;

        if (filter.DaysOfWeek != null && filter.DaysOfWeek.Any() && !filter.DaysOfWeek.Contains(when.DayOfWeek))
            return false;

        if (filter.StartDate > when || filter.EndDate < when)
            return false;

        return true;
    }
}