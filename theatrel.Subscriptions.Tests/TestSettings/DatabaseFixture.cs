using Autofac;
using System;
using theatrel.DataAccess;
using theatrel.DataAccess.DbService;
using theatrel.Lib;

namespace theatrel.Subscriptions.Tests.TestSettings
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

            containerBuilder.RegisterModule<SubscriptionModule>();

            RootScope = containerBuilder.Build();
        }

        public IDbService GetDbService() => RootScope.Resolve<IDbService>();

        public void Dispose()
        {
            RootScope.Dispose();
        }
    }
}
