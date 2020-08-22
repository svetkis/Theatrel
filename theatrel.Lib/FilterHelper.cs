using System;
using System.Linq;
using theatrel.Interfaces;

namespace theatrel.Lib
{
    public class FilterHelper : IFilterHelper
    {
        public IPerformanceFilter GetFilter(IChatDataInfo dataInfo)
        {
            var filter = new PerformanceFilter
            {
                StartDate = dataInfo.When,
                EndDate = dataInfo.When
            };

            if (dataInfo.Days != null && dataInfo.Days.Any())
            {
                var days = dataInfo.Days.Distinct().ToArray();
                if (days.Count() < 7)
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
    }
}