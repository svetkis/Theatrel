using System;
using System.Linq;
using theatrel.Interfaces;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot
{
    public class FilterHelper : IFilterHelper
    {
        private class PerformanceFilter : IPerformanceFilter
        {
            public DayOfWeek[] DaysOfWeek { get; set; }
            public string[] PerformanceTypes { get; set; }
            public string[] Locations { get; set; }

            public bool Filter(IPerformanceData perfomance)
            {
                throw new NotImplementedException();
            }
        }

        public IPerformanceFilter GetFilter(IChatDataInfo dataInfo)
        {
            var filter = new PerformanceFilter();

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
    }
}