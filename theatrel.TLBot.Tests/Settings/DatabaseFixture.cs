using Autofac;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using theatrel.DataAccess;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Playbill;
using theatrel.Lib;

namespace theatrel.TLBot.Tests.Settings;

public class DatabaseFixture : IDisposable
{
    public AppDbContext Db { get; private set; }
    public ILifetimeScope RootScope { get; private set; }

    public DatabaseFixture()
    {
        ContainerBuilder containerBuilder = new ContainerBuilder();

        containerBuilder.RegisterModule<TheatrelLibModule>();
        containerBuilder.RegisterModule<TheatrelDataAccessModule>();

        containerBuilder
            .RegisterType<TestDbContextOptionsFactory>()
            .AsImplementedInterfaces()
            .InstancePerDependency();

        var playBillResolverMock = new Mock<IPlayBillDataResolver>();
        playBillResolverMock.Setup(h => h.RequestProcess(It.IsAny<int>(), It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(Array.Empty<IPerformanceData>()));

        containerBuilder.RegisterInstance(playBillResolverMock.Object).As<IPlayBillDataResolver>().AsImplementedInterfaces();
        containerBuilder.RegisterModule<TlBotModule>();

        RootScope = containerBuilder.Build();
        Db = RootScope.Resolve<IDbService>().GetDbContext();
    }

    public void Dispose()
    {
        RootScope.Dispose();
        RootScope = null;

        Db.Dispose();
        Db = null;

        GC.SuppressFinalize(this);
    }
}