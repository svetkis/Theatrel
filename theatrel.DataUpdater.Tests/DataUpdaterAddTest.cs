using Autofac;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Common.Enums;
using theatrel.DataAccess.DbService;
using theatrel.DataUpdater.Tests.TestSettings;
using theatrel.Interfaces.DataUpdater;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Playbill;
using Xunit;

namespace theatrel.DataUpdater.Tests
{
    public class DataUpdaterAddTest : DataUpdaterTestBase
    {
        public DataUpdaterAddTest(DatabaseFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async void TestAdd()
        {
            string performanceUrl = "testAddUrl";
            string performanceName = "TestOpera1";
            string performanceLocation = "locAdd";
            string performanceType = "operaTestTypeAdd";

            DateTime performanceWhen = DateTime.Now.AddMonths(1);
            DateTime filterFrom = new DateTime(performanceWhen.Year, performanceWhen.Month, 1);
            DateTime filterTo = filterFrom.AddMonths(1).AddDays(-1);

            Mock<IPlayBillDataResolver> playBillResolverMock = new Mock<IPlayBillDataResolver>();
            playBillResolverMock.Setup(h =>
                    h.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new[]
                {
                    GetPerformanceMock(performanceName,0, performanceUrl, performanceWhen, performanceLocation, performanceType),
                    GetPerformanceMock("TestOpera2",0, "op2", DateTime.Now.AddDays(-2), performanceLocation, performanceType)
                }));

            await using var db = Fixture.RootScope.Resolve<IDbService>().GetDbContext();

            await using ILifetimeScope scope = Fixture.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(playBillResolverMock.Object).As<IPlayBillDataResolver>().AsImplementedInterfaces();

                builder.RegisterModule<DataUpdaterModule>();
            });

            var dataUpdater = scope.Resolve<IDbPlaybillUpdater>();

            await dataUpdater.UpdateAsync(1, filterFrom, filterTo, CancellationToken.None);

            var minPrice500 = GetPerformanceMock(
                performanceName, 500, performanceUrl, performanceWhen, performanceLocation, performanceType);

            playBillResolverMock.Setup(h =>
                    h.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new[]
                {
                    minPrice500
                }));

            await dataUpdater.UpdateAsync(1, new DateTime(2020, 9, 1), new DateTime(2020, 10, 1),
                CancellationToken.None);

            // nothing changed
            await dataUpdater.UpdateAsync(1, new DateTime(2020, 9, 1), new DateTime(2020, 10, 1),
                CancellationToken.None);

            var changes = db.PerformanceChanges
                .Where(c => c.PlaybillEntity.Url == performanceUrl)
                .OrderBy(d => d.LastUpdate).ToArray();

            Assert.Equal(3, changes.Count());
            Assert.Equal(1, db.PerformanceLocations.Count(l => l.Name == performanceLocation));
            Assert.Equal(1, db.PerformanceTypes.Count(t => t.TypeName == performanceType));
            Assert.Equal((int)ReasonOfChanges.NoReason, changes.Last().ReasonOfChanges);
            Assert.Equal((int)ReasonOfChanges.StartSales, changes[1].ReasonOfChanges);
            Assert.Equal((int)ReasonOfChanges.Creation, changes.First().ReasonOfChanges);
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
