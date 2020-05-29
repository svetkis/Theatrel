using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using theatrel.Interfaces;
using Xunit;

namespace theatrel.Tests
{
    public class FilterCheckerTest
    {
        internal class FilterData
        {
            public FilterData(object[] initData)
            {
                Days = initData[0] as DayOfWeek[];
                Types = initData[1] as string[];
            }

            public string[] Types { get; set; }
            public DayOfWeek[] Days { get; set; }
        }

        internal class PerfomanceData
        {
            public PerfomanceData(object[] initData)
            {
                Date = new DateTime((int)initData[0], (int)initData[1], (int)initData[2]);
                Type = initData.Last() as string;
            }

            public DateTime Date { get; set; }
            public string Type { get; set; }
        }

        [Theory]
        [InlineData( new object[] { 2020, 03, 23, "концерт" }, new object[] { new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }, new[] { "Концерт" } }, false)]
        [InlineData(new object[] { 2020, 03, 21, "балет" }, new object[] { new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }, new[] { "Концерт" } }, false)]
        [InlineData(new object[] { 2020, 03, 21, "концерт" }, new object[] { new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }, new[] { "Концерт" } }, true)]
        [InlineData(new object[] { 2020, 03, 20, "опера" }, new object[] { new[] { DayOfWeek.Friday, DayOfWeek.Sunday }, new[] { "Концерт" } }, false)]
        [InlineData(new object[] { 2020, 03, 20, "опера" }, new object[] { new[] { DayOfWeek.Friday, DayOfWeek.Sunday }, new[] { "Опера" } }, true)]
        public void Test(object[] perfomanceDataArr, object[] filterDataArr, bool expected)
        {
            var filterChecker = DIContainerHolder.Resolve<IFilterChecker>();
            var filter = DIContainerHolder.Resolve<IPerformanceFilter>();
            var perfomance = DIContainerHolder.Resolve<IPerformanceData>();

            var perfomanceData = new PerfomanceData(perfomanceDataArr);
            var filterData = new FilterData(filterDataArr);

            perfomance.DateTime = perfomanceData.Date;
            perfomance.Type = perfomanceData.Type;

            filter.DaysOfWeek = filterData.Days;
            filter.PerfomanceTypes = filterData.Types;

            Assert.True(filterChecker.IsDataSuitable(perfomance, filter) == expected);
        }
    }
}
