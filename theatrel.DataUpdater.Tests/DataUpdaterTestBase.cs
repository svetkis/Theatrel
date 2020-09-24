using Moq;
using System;
using theatrel.DataUpdater.Tests.TestSettings;
using theatrel.Interfaces.Playbill;
using Xunit;

namespace theatrel.DataUpdater.Tests
{
    public class DataUpdaterTestBase : IClassFixture<DatabaseFixture>
    {
        protected readonly DatabaseFixture Fixture;
        public DataUpdaterTestBase(DatabaseFixture fixture)
        {
            Fixture = fixture;
        }

        protected IPerformanceData GetPerformanceMock(string name, int minPrice, string url, DateTime performanceDateTime, string location, string type)
        {
            Mock<IPerformanceData> performanceMock = new Mock<IPerformanceData>();

            performanceMock.SetupGet(x => x.Name).Returns(name);
            performanceMock.SetupGet(x => x.Type).Returns(type);
            performanceMock.SetupGet(x => x.Location).Returns(location);
            performanceMock.SetupGet(x => x.MinPrice).Returns(minPrice);

            performanceMock.SetupGet(x => x.TicketsUrl).Returns(url);
            performanceMock.SetupGet(x => x.DateTime).Returns(performanceDateTime);

            return performanceMock.Object;
        }
    }
}
