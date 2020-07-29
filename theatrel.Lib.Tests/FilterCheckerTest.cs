using System;
using System.Linq;
using theatrel.Interfaces;
using theatrel.Tests;
using Xunit;

namespace theatrel.Lib.Tests
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

        internal class PerformanceData
        {
            public PerformanceData(object[] initData)
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
            var performance = DIContainerHolder.Resolve<IPerformanceData>();

            var performanceData = new PerformanceData(perfomanceDataArr);
            var filterData = new FilterData(filterDataArr);

            performance.DateTime = performanceData.Date;
            performance.Type = performanceData.Type;

            filter.DaysOfWeek = filterData.Days;
            filter.PerformanceTypes = filterData.Types;

            Assert.Equal(expected, filterChecker.IsDataSuitable(performance, filter));
        }
    }
}
