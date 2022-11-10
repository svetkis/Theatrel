using System;
using System.Linq;
using theatrel.Interfaces.Filters;
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
                LocationIds = dataInfo.LocationIds
            };
        }

        if (!string.IsNullOrEmpty(dataInfo.Actor))
        {
            return new PerformanceFilter
            {
                Actor = dataInfo.Actor
            };
        }

        var filter = new PerformanceFilter
        {
            StartDate = dataInfo.When,
            EndDate = dataInfo.When.AddMonths(1)
        };

        if (dataInfo.TheatreIds != null && dataInfo.TheatreIds.Any())
            filter.TheatreIds = dataInfo.TheatreIds;

        if (dataInfo.LocationIds != null && dataInfo.LocationIds.Any())
            filter.LocationIds = dataInfo.LocationIds;

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

    public bool IsDataSuitable(string name, string cast, int locationId, string type, DateTime when, IPerformanceFilter filter)
    {
        return IsDataSuitable(-1, name, cast, locationId, type, when, filter);
    }

    public bool CheckOnlyDate(DateTime when, IPerformanceFilter filter)
    {
        if (filter == null)
            return true;

        return filter.StartDate <= when && filter.EndDate >= when;
    }

    private static bool CheckLocation(int[] filterLocations, int locationId)
    {
        if (filterLocations == null || !filterLocations.Any())
            return true;

        return filterLocations.Contains(locationId);
    }

    public bool IsDataSuitable(int playbillEntryId, string name, string cast, int locationId, string type, DateTime when, IPerformanceFilter filter)
    {
        if (filter == null)
            return true;

        //check particalar Playbill
        if (filter.PlaybillId != -1 && playbillEntryId != -1)
            return filter.PlaybillId == playbillEntryId;

        //check by actor
        if (!string.IsNullOrEmpty(filter.Actor))
        {
            return cast.ToLower().Contains(filter.Actor.ToLower());
        }

        if (!string.IsNullOrEmpty(filter.PerformanceName))
        {
            return name.ToLower().Contains(filter.PerformanceName.ToLower()) && CheckLocation(filter.LocationIds, locationId);
        }

        if (!CheckLocation(filter.LocationIds, locationId))
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