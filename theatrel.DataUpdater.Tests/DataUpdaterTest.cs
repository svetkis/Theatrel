using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Moq;
using theatrel.Common.Enums;
using theatrel.DataAccess;
using theatrel.Interfaces;
using Xunit;

namespace theatrel.DataUpdater.Tests
{
    public class DataUpdaterTest
    {
        private IPerformanceData GetPerformance(int minPrice, string url, DateTime performanceDateTime)
        {
            Mock<IPerformanceData> performanceMock = new Mock<IPerformanceData>();

            performanceMock.SetupGet(x => x.MinPrice).Returns(minPrice);

            performanceMock.SetupGet(x => x.Url).Returns(url);
            performanceMock.SetupGet(x => x.DateTime).Returns(performanceDateTime);

            return performanceMock.Object;
        }

        [Fact]
        public async void TestAdd()
        {
            Mock<IPlayBillDataResolver> playBillResolverMock = new Mock<IPlayBillDataResolver>();
            playBillResolverMock.Setup(h => h.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new[]
                {
                    GetPerformance(0, "testUrl", new DateTime(2020, 9, 10 ))
                }));

            await using ILifetimeScope scope = DIContainerHolder.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(playBillResolverMock.Object).As<IPlayBillDataResolver>().AsImplementedInterfaces();
            });

            var dataUpdater = scope.Resolve<IDataUpdater>();

            await dataUpdater.UpdateAsync(1, new DateTime(2020, 9, 1), new DateTime(2020, 10, 1), CancellationToken.None);

            var minPrice500 = GetPerformance(500, "testUrl", new DateTime(2020, 9, 10));
            playBillResolverMock.Setup(h => h.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new IPerformanceData[]
                {
                    minPrice500
                }));

            await dataUpdater.UpdateAsync(1, new DateTime(2020, 9, 1), new DateTime(2020, 10, 1), CancellationToken.None);
            // nothing changed
            await dataUpdater.UpdateAsync(1, new DateTime(2020, 9, 1), new DateTime(2020, 10, 1), CancellationToken.None);

            var db = scope.Resolve<AppDbContext>();
            var changes = db.PerformanceChanges.OrderBy(d => d.LastUpdate);
            Assert.Equal(2, db.PerformanceChanges.Count());
            Assert.Equal((int)ReasonOfChanges.StartSales, changes.Last().ReasonOfChanges);
            Assert.Equal((int)ReasonOfChanges.Creation, changes.First().ReasonOfChanges);
        }
    }
}
