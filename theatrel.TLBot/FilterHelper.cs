using System;
using System.Linq;
using theatrel.Interfaces;
using theatrel.TLBot.Interfaces;

namespace theatrel.TLBot
{
    public class FilterHelper : IFilterHelper
    {
        private IPerformanceFilter _filter;

        public FilterHelper(IPerformanceFilter filter)
        {
            _filter = filter;
        }

        public IPerformanceFilter GetFilter(IChatDataInfo dataInfo)
        {
            if (dataInfo.Days != null && dataInfo.Days.Any())
            {
                var days = dataInfo.Days.Distinct();
                if (days.Count() < 7)
                    _filter.DaysOfWeek = days.ToArray();
            }

            if (dataInfo.Types != null && dataInfo.Types.Any())
                _filter.PerfomanceTypes = dataInfo.Types;

            return _filter;
        }
    }
}