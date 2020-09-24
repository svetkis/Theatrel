using Autofac;
using JetBrains.Profiler.Api;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataUpdater;
using theatrel.Interfaces.DataUpdater;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Playbill;

namespace theatrel.ConsoleTest
{
    internal class UpdaterTest
    {
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
                    h.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new[]
                {
                    GetPerformanceMock(performanceName,0, performanceUrl, performanceWhen, performanceLocation, performanceType),
                    GetPerformanceMock("TestOpera2",0, "op2", DateTime.Now.AddDays(-2), performanceLocation, performanceType)
                }));

            var minPrice500 = GetPerformanceMock(
                performanceName, 500, performanceUrl, performanceWhen, performanceLocation, performanceType);

            var minPrice300 = GetPerformanceMock(
                performanceName, 300, performanceUrl, performanceWhen, performanceLocation, performanceType);

            await using ILifetimeScope testScope = Bootstrapper.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(playBillResolverMock.Object).As<IPlayBillDataResolver>().AsImplementedInterfaces();

                builder.RegisterModule<DataUpdaterModule>();
            });

            GC.Collect();
            MemoryProfiler.GetSnapshot();

            await using (var internalScope = testScope.BeginLifetimeScope())
            {
                var dataUpdater = internalScope.Resolve<IDbPlaybillUpdater>();
                await dataUpdater.UpdateAsync(1, filterFrom, filterTo, CancellationToken.None);
            }

            GC.Collect();
            MemoryProfiler.GetSnapshot();

            //setup new values
            playBillResolverMock.Setup(h =>
                    h.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
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

            GC.Collect();
            MemoryProfiler.GetSnapshot();

            //setup new values
            playBillResolverMock.Setup(h =>
                    h.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new[]
                {
                    minPrice300
                }));

            //price changed
            await using (var internalScope = testScope.BeginLifetimeScope())
            {
                var dataUpdater = internalScope.Resolve<IDbPlaybillUpdater>();
                await dataUpdater.UpdateAsync(1, filterFrom, filterTo, CancellationToken.None);
            }

            await using (var internalScope = testScope.BeginLifetimeScope())
            {
                var dataUpdater = internalScope.Resolve<IDbPlaybillUpdater>();
                await dataUpdater.UpdateAsync(1, filterFrom, filterTo, CancellationToken.None);
            }
        }

        private IPerformanceData GetPerformanceMock(string name, int minPrice, string url, DateTime performanceDateTime, string location, string type)
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
