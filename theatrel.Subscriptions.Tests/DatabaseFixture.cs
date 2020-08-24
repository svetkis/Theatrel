using System;
using Autofac;
using theatrel.DataAccess;
using theatrel.Lib;

namespace theatrel.Subscriptions.Tests
{
    public class DatabaseFixture : IDisposable
    {
        public AppDbContext Db { get; }
        public ILifetimeScope RootScope { get; }

        public DatabaseFixture()
        {
            Db = new AppDbContext(TestDbContextOptionsFactory.Get());

            ContainerBuilder containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule<TheatrelLibModule>();

            containerBuilder.RegisterInstance(Db).As<AppDbContext>();

            containerBuilder.RegisterModule<SubscriptionModule>();

            RootScope = containerBuilder.Build();
        }

        public void Dispose()
        {
            RootScope.Dispose();
        }
    }
}
