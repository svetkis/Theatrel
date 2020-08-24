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
            string performanceName = "TestOpera1";
            string performanceLocation = "locAdd";
            string performanceType = "operaTestTypeAdd";

            Mock<IPlayBillDataResolver> playBillResolverMock = new Mock<IPlayBillDataResolver>();
            playBillResolverMock.Setup(h =>
                    h.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new[]
                {
                    GetPerformanceMock(performanceName,0, performanceUrl, new DateTime(2020, 9, 10), performanceLocation, performanceType),
                    GetPerformanceMock("TestOpera2",0, "op2", new DateTime(2020, 9, 11), performanceLocation, performanceType)
                }));

            await using ILifetimeScope scope = DIContainerHolder.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(playBillResolverMock.Object).As<IPlayBillDataResolver>()
                    .AsImplementedInterfaces();
            });

            var dataUpdater = scope.Resolve<IDataUpdater>();

            await dataUpdater.UpdateAsync(1, new DateTime(2020, 9, 1), new DateTime(2020, 10, 1),
                CancellationToken.None);

            var minPrice500 = GetPerformanceMock(
                performanceName,500, performanceUrl, new DateTime(2020, 9, 10), performanceLocation, performanceType);

            playBillResolverMock.Setup(h =>
                    h.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new[]
                {
                    minPrice500
                }));

            await dataUpdater.UpdateAsync(1, new DateTime(2020, 9, 1), new DateTime(2020, 10, 1),
                CancellationToken.None);

            await Task.Delay(1000);
            // nothing changed
            await dataUpdater.UpdateAsync(1, new DateTime(2020, 9, 1), new DateTime(2020, 10, 1),
                CancellationToken.None);

            var db = scope.Resolve<AppDbContext>();
            var changes = db.PerformanceChanges
                .Where(c => c.PlaybillEntity.Url == performanceUrl)
                .OrderBy(d => d.LastUpdate);

            Assert.Equal(2, changes.Count());
            Assert.Equal(1, db.PerformanceLocations.Count(l => l.Name == performanceLocation));
            Assert.Equal(1, db.PerformanceTypes.Count(t => t.TypeName == performanceType));
            Assert.Equal((int) ReasonOfChanges.StartSales, changes.Last().ReasonOfChanges);
            Assert.Equal((int) ReasonOfChanges.Creation, changes.First().ReasonOfChanges);
        }

        [Fact]
        public void TestString()
        {
            string str1 = "https://tickets.mariinsky.ru/ru/performance/a0QxYXcveDVZK1dwM1Q4dm03TzBTZz09/";
            string str2 = "https://tickets.mariinsky.ru/ru/performance/a0QxYXcveDVZK1dwM1Q4dm03TzBTZz09/";

            Assert.True(string.Compare(str1, str2, StringComparison.InvariantCultureIgnoreCase) == 0);
        }
    }
}
