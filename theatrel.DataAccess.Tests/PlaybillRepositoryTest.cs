using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Moq;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Structures.Entities;
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
        }

        [Fact]
        public async Task AddTest()
        {
            await ConfigureDb();

            using var pbRepository = Fixture.RootScope.Resolve<IDbService>().GetPlaybillRepository();

            var performance2 = GetPerformanceMock("TestPerformance2", 800, "url2", DateTime.Now,  TestLocationName, TestTypeName);
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
            await ConfigureDb();

            using var pbRepository = Fixture.RootScope.Resolve<IDbService>().GetPlaybillRepository();

            var performance4 = GetPerformanceMock("TestPerformance4", 800, "url4", DateTime.Now, TestLocationName, TestTypeName);

            var pb4 = await pbRepository.AddPlaybill(performance4);
            var change = pb4.Changes.Last();
            var dt = DateTime.Now;
            change.LastUpdate = dt;
            bool updateResult = await pbRepository.Update(change);

            Assert.Equal(dt, pbRepository.Get(performance4).Changes.Last().LastUpdate);
            Assert.NotNull(pb4);
            Assert.True(updateResult);
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

            performanceMock.SetupGet(x => x.Url).Returns(url);
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
