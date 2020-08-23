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
    public class DataUpdaterTestAddTest : DataUpdaterTestBase
    {
        [Fact]
        public async void TestAdd()
        {
            string performanceUrl = "testAddUrl";

            Mock<IPlayBillDataResolver> playBillResolverMock = new Mock<IPlayBillDataResolver>();
            playBillResolverMock.Setup(h => h.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new[]
                {
                    GetPerformanceMock(0, performanceUrl, new DateTime(2020, 9, 10 ))
                }));

            await using ILifetimeScope scope = DIContainerHolder.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(playBillResolverMock.Object).As<IPlayBillDataResolver>().AsImplementedInterfaces();
            });

            var dataUpdater = scope.Resolve<IDataUpdater>();

            await dataUpdater.UpdateAsync(1, new DateTime(2020, 9, 1), new DateTime(2020, 10, 1), CancellationToken.None);

            var minPrice500 = GetPerformanceMock(500, performanceUrl, new DateTime(2020, 9, 10));
            playBillResolverMock.Setup(h =>
                    h.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new[]
                {
                    minPrice500
                }));

            await dataUpdater.UpdateAsync(1, new DateTime(2020, 9, 1), new DateTime(2020, 10, 1), CancellationToken.None);
            // nothing changed
            await dataUpdater.UpdateAsync(1, new DateTime(2020, 9, 1), new DateTime(2020, 10, 1), CancellationToken.None);

            var db = scope.Resolve<AppDbContext>();
            var changes = db.PerformanceChanges
                .Where(c => c.PerformanceEntity.Url == performanceUrl)
                .OrderBy(d => d.LastUpdate);

            Assert.Equal(2, changes.Count());
            Assert.Equal((int)ReasonOfChanges.StartSales, changes.Last().ReasonOfChanges);
            Assert.Equal((int)ReasonOfChanges.Creation, changes.First().ReasonOfChanges);
        }
    }
}
