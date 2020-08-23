using System;
using Moq;
using theatrel.Interfaces;

namespace theatrel.DataUpdater.Tests
{
    public class DataUpdaterTestBase
    {
        protected IPerformanceData GetPerformanceMock(int minPrice, string url, DateTime performanceDateTime)
        {
            Mock<IPerformanceData> performanceMock = new Mock<IPerformanceData>();

            performanceMock.SetupGet(x => x.MinPrice).Returns(minPrice);

            performanceMock.SetupGet(x => x.Url).Returns(url);
            performanceMock.SetupGet(x => x.DateTime).Returns(performanceDateTime);

            return performanceMock.Object;
        }
    }
}
