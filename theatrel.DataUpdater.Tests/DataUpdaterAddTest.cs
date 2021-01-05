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
        public async Task TestAdd()
        {
            string performanceUrl = "testAddUrl";
            string performanceName = "TestOpera1";
            string performanceLocation = "locAdd";
            string performanceType = "operaTestTypeAdd";

            DateTime performanceWhen = DateTime.Now.AddMonths(1);
            DateTime filterFrom = new DateTime(performanceWhen.Year, performanceWhen.Month, 1);
            DateTime filterTo = filterFrom.AddMonths(1);

            Mock<IPlayBillDataResolver> playBillResolverMock = new Mock<IPlayBillDataResolver>();
            playBillResolverMock.Setup(h =>
                    h.RequestProcess(It.IsAny<int>(), It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new[]
                {
                    GetPerformanceMock(performanceName,0, performanceUrl, performanceWhen, performanceLocation, performanceType),
                    GetPerformanceMock("TestOpera2",0, "op2", DateTime.Now.AddDays(-2), performanceLocation, performanceType)
                }));

            var minPrice500 = GetPerformanceMock(
                performanceName, 500, performanceUrl, performanceWhen, performanceLocation, performanceType);

            await using ILifetimeScope testScope = Fixture.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(playBillResolverMock.Object).As<IPlayBillDataResolver>().AsImplementedInterfaces();

                builder.RegisterModule<DataUpdaterModule>();
            });

            await using (var internalScope = testScope.BeginLifetimeScope())
            {
                var dataUpdater = internalScope.Resolve<IDbPlaybillUpdater>();
                await dataUpdater.UpdateAsync(1, filterFrom, filterTo, CancellationToken.None);
            }

            //setup new values
            playBillResolverMock.Setup(h =>
                    h.RequestProcess(It.IsAny<int>(), It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new[]
                {
                    minPrice500
                }));

            //price changed

            await using (var internalScope = testScope.BeginLifetimeScope())
            {
                var dataUpdater = internalScope.Resolve<IDbPlaybillUpdater>();
                await dataUpdater.UpdateAsync(1, filterFrom, filterTo, CancellationToken.None);
            }

            // nothing changed
            await using (var internalScope = testScope.BeginLifetimeScope())
            {
                var dataUpdater = internalScope.Resolve<IDbPlaybillUpdater>();
                await dataUpdater.UpdateAsync(1, filterFrom, filterTo, CancellationToken.None);
            }

            //check
            await using var db = Fixture.RootScope.Resolve<IDbService>().GetDbContext();

            var changes = db.PlaybillChanges
                .Where(c => c.PlaybillEntity.TicketsUrl == performanceUrl)
                .OrderBy(d => d.LastUpdate).ToArray();

            Assert.Equal(2, changes.Length);
            Assert.Equal(1, db.PerformanceLocations.Count(l => l.Name == performanceLocation));
            Assert.Equal(1, db.PerformanceTypes.Count(t => t.TypeName == performanceType));
            Assert.Equal((int)ReasonOfChanges.Creation, changes.First().ReasonOfChanges);
            Assert.Equal((int)ReasonOfChanges.StartSales, changes.Last().ReasonOfChanges);
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
