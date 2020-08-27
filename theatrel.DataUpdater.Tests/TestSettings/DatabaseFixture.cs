using Autofac;
using System;
using theatrel.DataAccess;
using theatrel.DataAccess.DbService;
using theatrel.Lib;

namespace theatrel.DataUpdater.Tests.TestSettings
{
    public class DatabaseFixture : IDisposable
    {
        public ILifetimeScope RootScope { get; }

        public DatabaseFixture()
        {
            ContainerBuilder containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule<TheatrelLibModule>();

            containerBuilder.RegisterModule<TheatrelDataAccessModule>();

            containerBuilder
                .RegisterType<TestDbContextOptionsFactory>()
                .AsImplementedInterfaces()
                .InstancePerDependency();

            containerBuilder.RegisterModule<DataUpdaterModule>();

            RootScope = containerBuilder.Build();
        }

        public AppDbContext GetDb() => RootScope.Resolve<IDbService>().GetDbContext();

        public void Dispose()
        {
            RootScope.Dispose();
        }
    }
}
