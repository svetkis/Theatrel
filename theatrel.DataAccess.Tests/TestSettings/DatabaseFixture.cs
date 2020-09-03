using System;
using Autofac;
using theatrel.DataAccess.DbService;

namespace theatrel.DataAccess.Tests.TestSettings
{
    public class DatabaseFixture : IDisposable
    {
        public ILifetimeScope RootScope { get; }

        public DatabaseFixture()
        {
            ContainerBuilder containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule<TheatrelDataAccessModule>();

            containerBuilder
                .RegisterType<TestDbContextOptionsFactory>()
                .AsImplementedInterfaces()
                .InstancePerDependency();

            RootScope = containerBuilder.Build();
        }

        public AppDbContext GetDb() => RootScope.Resolve<IDbService>().GetDbContext();

        public void Dispose()
        {
            RootScope.Dispose();
        }
    }

}
