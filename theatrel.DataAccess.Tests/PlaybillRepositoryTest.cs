using Autofac;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using theatrel.Common.Enums;
using theatrel.DataAccess.DbService;
using theatrel.DataAccess.Tests.TestSettings;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.Playbill;
using Xunit;

namespace theatrel.DataAccess.Tests;

public class PlaybillRepositoryTest : IClassFixture<DatabaseFixture>
{
    protected readonly DatabaseFixture Fixture;
    public PlaybillRepositoryTest(DatabaseFixture fixture)
    {
        Fixture = fixture;
        ConfigureDb().Wait();
    }

    [Fact]
    public async Task AddTest()
    {
        using var pbRepository = Fixture.RootScope.Resolve<IDbService>().GetPlaybillRepository();

        var performance2 = GetPerformanceMock("TestPerformance2", 800, "url2", DateTime.UtcNow, TestLocationName, TestTypeName);
        var performance3 = GetPerformanceMock("TestPerformance3", 800, "url3", DateTime.UtcNow, TestLocationName, TestTypeName);

        var exceptions = await Record.ExceptionAsync(async () =>
        {
            var res2 = await pbRepository.AddPlaybill(performance2, (int)ReasonOfChanges.StartSales);
            var res3 = await pbRepository.AddPlaybill(performance3, (int)ReasonOfChanges.StartSales);
            Assert.NotNull(res2);
            Assert.NotNull(res3);
        });

        Assert.Null(exceptions);
    }

    [Fact]
    public async Task UpdateTest()
    {
        using var pbRepository = Fixture.RootScope.Resolve<IDbService>().GetPlaybillRepository();

        var performance4 = GetPerformanceMock("TestPerformance4", 800, "url4", DateTime.UtcNow, TestLocationName, TestTypeName);

        var pb4 = await pbRepository.AddPlaybill(performance4, (int)ReasonOfChanges.StartSales);
        var change = pb4.Changes.Last();
        bool updateResult = await pbRepository.UpdateChangeLastUpdate(change.Id);

        Assert.NotNull(pb4);
        Assert.True(updateResult);
    }

    [Fact]
    public async Task GetListTest()
    {
        using var pbRepository = Fixture.RootScope.Resolve<IDbService>().GetPlaybillRepository();

        var performance2 = GetPerformanceMock("TestPerformance2", 500, "url2", DateTime.UtcNow.AddDays(50), TestLocationName, TestTypeName);

        await pbRepository.AddPlaybill(performance2, (int)ReasonOfChanges.StartSales);

        var list = pbRepository.GetList(DateTime.UtcNow.AddDays(49), DateTime.UtcNow.AddDays(51));

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

        Mock<IActor> actor1Mock = new Mock<IActor>();
        actor1Mock.SetupGet(a => a.Name).Returns("actor1");
        actor1Mock.SetupGet(a => a.Url).Returns("actor1url");

        Mock<IActor> actor2Mock = new Mock<IActor>();
        actor2Mock.SetupGet(a => a.Name).Returns("actor2");
        actor2Mock.SetupGet(a => a.Url).Returns("actor2url");

        IList<IActor> actors = new List<IActor> {actor1Mock.Object, actor2Mock.Object};

        Mock<IPerformanceCast> castMock = new Mock<IPerformanceCast>();
        castMock.SetupGet(x => x.State).Returns(CastState.Ok);
        castMock.SetupGet(x => x.Cast).Returns(new Dictionary<string, IList<IActor>>{ {"role1" , actors}});
        performanceMock.SetupGet(x => x.Cast).Returns(castMock.Object);

        return performanceMock.Object;
    }


    private async Task ConfigureDb()
    {
        using var pbRepository = Fixture.RootScope.Resolve<IDbService>().GetPlaybillRepository();

        var performance = GetPerformanceMock("TestPerformance", 500, "url", DateTime.UtcNow, TestLocationName, TestTypeName);

        await pbRepository.AddPlaybill(performance, (int)ReasonOfChanges.StartSales);
    }
}