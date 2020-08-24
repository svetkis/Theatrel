using System;
using Moq;
using theatrel.Interfaces;

namespace theatrel.DataUpdater.Tests
{
    public class DataUpdaterTestBase
    {
        protected IPerformanceData GetPerformanceMock(string name, int minPrice, string url, DateTime performanceDateTime, string location, string type)
        {
            Mock<IPerformanceData> performanceMock = new Mock<IPerformanceData>();

            performanceMock.SetupGet(x => x.Name).Returns(name);
            performanceMock.SetupGet(x => x.Type).Returns(type);
            performanceMock.SetupGet(x => x.Location).Returns(location);
            performanceMock.SetupGet(x => x.MinPrice).Returns(minPrice);

            performanceMock.SetupGet(x => x.Url).Returns(url);
            performanceMock.SetupGet(x => x.DateTime).Returns(performanceDateTime);

            return performanceMock.Object;
        }
    }
}
