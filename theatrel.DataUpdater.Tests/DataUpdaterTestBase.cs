using Moq;
using System;
using System.Collections.Generic;
using theatrel.Common.Enums;
using theatrel.DataUpdater.Tests.TestSettings;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.Playbill;
using Xunit;

namespace theatrel.DataUpdater.Tests;

public class DataUpdaterTestBase : IClassFixture<DatabaseFixture>
{
    protected readonly DatabaseFixture Fixture;
    public DataUpdaterTestBase(DatabaseFixture fixture)
    {
        Fixture = fixture;
    }

    protected IPerformanceData GetPerformanceMock(string name, int minPrice, string url, DateTime performanceDateTime, string location, string type)
    {
        Mock<IPerformanceData> performanceMock = new Mock<IPerformanceData>();

        performanceMock.SetupGet(x => x.Name).Returns(name);
        performanceMock.SetupGet(x => x.Type).Returns(type);
        performanceMock.SetupGet(x => x.Location).Returns(location);
        performanceMock.SetupGet(x => x.TheatreId).Returns(1);
        performanceMock.SetupGet(x => x.TheatreName).Returns("Мариинский театр");
        performanceMock.SetupGet(x => x.MinPrice).Returns(minPrice);

        performanceMock.SetupGet(x => x.TicketsUrl).Returns(url);
        performanceMock.SetupGet(x => x.DateTime).Returns(performanceDateTime);

        if (minPrice == 0)
            performanceMock.SetupGet(x => x.State).Returns(TicketsState.NoTickets);
        else
            performanceMock.SetupGet(x => x.State).Returns(TicketsState.Ok);

        Mock<IActor> actor1Mock = new Mock<IActor>();
        actor1Mock.SetupGet(a => a.Name).Returns("actor1");
        actor1Mock.SetupGet(a => a.Url).Returns("actor1url");

        Mock<IActor> actor2Mock = new Mock<IActor>();
        actor2Mock.SetupGet(a => a.Name).Returns("actor2");
        actor2Mock.SetupGet(a => a.Url).Returns("actor2url");

        IList<IActor> actors = new List<IActor> { actor1Mock.Object, actor2Mock.Object };

        Mock<IActor> actor3Mock = new Mock<IActor>();
        actor3Mock.SetupGet(a => a.Name).Returns("actor3");
        actor3Mock.SetupGet(a => a.Url).Returns("actor3url");

        Mock<IPerformanceCast> castMock = new Mock<IPerformanceCast>();
        castMock.SetupGet(x => x.State).Returns(CastState.Ok);
        castMock.SetupGet(x => x.Cast).Returns(new Dictionary<string, IList<IActor>>
        {
            { "role1", actors },
            { "role2", new List<IActor>{actor3Mock.Object} },
        });
        performanceMock.SetupGet(x => x.Cast).Returns(castMock.Object);


        return performanceMock.Object;
    }
}