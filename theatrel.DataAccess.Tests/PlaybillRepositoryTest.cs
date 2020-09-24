using Autofac;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Tests.TestSettings;
using theatrel.Interfaces.Playbill;
using Xunit;

namespace theatrel.DataAccess.Tests
{
    public class PlaybillRepositoryTest : IClassFixture<DatabaseFixture>
    {
        protected readonly DatabaseFixture Fixture;
        public PlaybillRepositoryTest(DatabaseFixture fixture)
        {
            Fixture = fixture;
            Task.WaitAll(ConfigureDb());
        }

        [Fact]
        public async Task AddTest()
        {
            using var pbRepository = Fixture.RootScope.Resolve<IDbService>().GetPlaybillRepository();

            var performance2 = GetPerformanceMock("TestPerformance2", 800, "url2", DateTime.Now, TestLocationName, TestTypeName);
            var performance3 = GetPerformanceMock("TestPerformance3", 800, "url3", DateTime.Now, TestLocationName, TestTypeName);

            var exceptions = await Record.ExceptionAsync(async () =>
            {
                var res2 = await pbRepository.AddPlaybill(performance2);
                var res3 = await pbRepository.AddPlaybill(performance3);
                Assert.NotNull(res2);
                Assert.NotNull(res3);
            });

            Assert.Null(exceptions);
        }

        [Fact]
        public async Task UpdateTest()
        {
            using var pbRepository = Fixture.RootScope.Resolve<IDbService>().GetPlaybillRepository();

            var performance4 = GetPerformanceMock("TestPerformance4", 800, "url4", DateTime.Now, TestLocationName, TestTypeName);

            var pb4 = await pbRepository.AddPlaybill(performance4);
            var change = pb4.Changes.Last();
            bool updateResult = await pbRepository.UpdateChangeLastUpdate(change.Id);

            Assert.NotNull(pb4);
            Assert.True(updateResult);
        }

        [Fact]
        public async Task GetListTest()
        {
            using var pbRepository = Fixture.RootScope.Resolve<IDbService>().GetPlaybillRepository();

            var performance2 = GetPerformanceMock("TestPerformance2", 500, "url2", DateTime.Now.AddDays(50), TestLocationName, TestTypeName);

            await pbRepository.AddPlaybill(performance2);

            var list = pbRepository.GetList(DateTime.Now.AddDays(49), DateTime.Now.AddDays(51));

            Assert.NotNull(list);
            Assert.Single(list);
        }


        private const string TestLocationName = "TestLocation";
        private const string TestTypeName = "TestType";

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


        private async Task ConfigureDb()
        {
            using var pbRepository = Fixture.RootScope.Resolve<IDbService>().GetPlaybillRepository();

            var performance = GetPerformanceMock("TestPerformance", 500, "url", DateTime.Now, TestLocationName, TestTypeName);

            await pbRepository.AddPlaybill(performance);
        }
    }
}
