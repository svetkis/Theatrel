using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using Moq;
using theatrel.Interfaces;
using theatrel.TLBot.Interfaces;
using Xunit;

namespace theatrel.DataUpdater.Tests
{
    public class DataUpdaterTest
    {
        public DataUpdaterTest()
        {

        }

        private IPerformanceData GetPerformance(int minPrice, string url, DateTime performanceDateTime)
        {
            Mock<IPerformanceTickets> ticketsMock = new Mock<IPerformanceTickets>();
            ticketsMock.Setup(x => x.GetMinPrice()).Returns(minPrice);

            Mock<IPerformanceData> performanceMock = new Mock<IPerformanceData>();
            performanceMock.SetupGet(x => x.Tickets).Returns(ticketsMock.Object);
            performanceMock.SetupGet(x => x.Url).Returns(url);
            performanceMock.SetupGet(x => x.DateTime).Returns(performanceDateTime);

            return performanceMock.Object;
        }

        [Fact]
        public async void TestAdd()
        {
            Mock<IPlayBillDataResolver> playBillResolverMock = new Mock<IPlayBillDataResolver>();
            playBillResolverMock.Setup(h => h.RequestProcess(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new IPerformanceData[]
                {
                    GetPerformance(0, "testUrl", new DateTime(2020, 9, 10 ))
                }));

            await using ILifetimeScope scope = DIContainerHolder.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(playBillResolverMock.Object).As<IPlayBillDataResolver>().AsImplementedInterfaces();
            });

            var dataUpdater = scope.Resolve<IDataUpdater>();

            await dataUpdater.UpdateAsync(1, new DateTime(2020, 9, 1), new DateTime(2020, 10, 1), CancellationToken.None);

            playBillResolverMock.Setup(h => h.RequestProcess(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new IPerformanceData[]
                {
                    GetPerformance(500, "testUrl", new DateTime(2020, 9, 10 ))
                }));

            await dataUpdater.UpdateAsync(1, new DateTime(2020, 9, 1), new DateTime(2020, 10, 1), CancellationToken.None);
        }
    }
}
