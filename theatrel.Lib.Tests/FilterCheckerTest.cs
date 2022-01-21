using Moq;
using System;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Playbill;
using Xunit;

namespace theatrel.Lib.Tests;

public class FilterCheckerTest
{
    [Theory]
    [InlineData(false, new[] { 2020, 03, 23 }, "концерт", new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }, new[] { "Концерт" })]
    [InlineData(false, new[] { 2020, 03, 21 }, "балет", new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }, new[] { "Концерт" })]
    [InlineData(true, new[] { 2020, 03, 21 }, "концерт", new[] { DayOfWeek.Saturday, DayOfWeek.Sunday }, new[] { "Концерт", "Балет" })]
    [InlineData(false, new[] { 2020, 03, 20 }, "опера", new[] { DayOfWeek.Friday, DayOfWeek.Sunday }, new[] { "Концерт" })]
    [InlineData(true, new[] { 2020, 03, 20 }, "опера", new[] { DayOfWeek.Friday, DayOfWeek.Sunday }, new[] { "Опера" })]
    public void Test(bool expected, int[] performanceDate, string performanceType, DayOfWeek[] filterDays, string[] filterTypes)
    {
        var dt = new DateTime(performanceDate[0], performanceDate[1], performanceDate[2], 0, 0, 0, DateTimeKind.Utc);

        var filterChecker = DIContainerHolder.Resolve<IFilterService>();

        var filter = new Mock<IPerformanceFilter>();
        filter.SetupGet(x => x.PerformanceName).Returns(string.Empty);
        filter.SetupGet(x => x.DaysOfWeek).Returns(filterDays);
        filter.SetupGet(x => x.PerformanceTypes).Returns(filterTypes);
        filter.SetupGet(x => x.Locations).Returns(Array.Empty<string>());
        filter.SetupGet(x => x.StartDate).Returns(dt.AddDays(-2));
        filter.SetupGet(x => x.EndDate).Returns(dt.AddDays(2));

        var performance = new Mock<IPerformanceData>();
        performance.SetupGet(x => x.DateTime).Returns(dt);
        performance.SetupGet(x => x.Type).Returns(performanceType);

        bool result = filterChecker.IsDataSuitable(performance.Object, filter.Object);

        Assert.Equal(expected, result);
    }
}