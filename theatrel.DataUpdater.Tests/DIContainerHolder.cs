using System;
using Autofac;
using Microsoft.EntityFrameworkCore;
using theatrel.DataAccess;
using theatrel.Lib;

namespace theatrel.DataUpdater.Tests
{
    public class DIContainerHolder
    {
        private static readonly Lazy<DIContainerHolder> DIContainer = new Lazy<DIContainerHolder>(() => new DIContainerHolder());
        private readonly ILifetimeScope _rootScope;

        public static ILifetimeScope RootScope => DIContainer.Value._rootScope;

        private DIContainerHolder()
        {
            ContainerBuilder containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule<DataResolverTestModule>();

            _rootScope = containerBuilder.Build();
        }
    }

    public class DataResolverTestModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<TheatrelLibModule>();
            builder.RegisterModule<DataUpdaterModule>();

            builder
                .RegisterType<AppDbContext>()
                .WithParameter("options", TestDbContextOptionsFactory.Get())
                .InstancePerLifetimeScope();

            base.Load(builder);
        }
    }

    public class TestDbContextOptionsFactory
    {
        public static DbContextOptions<AppDbContext> Get()
        {
            var builder = new DbContextOptionsBuilder<AppDbContext>();
            TestDbContextConfigurator.Configure(builder);

            return builder.Options;
        }
    }

    public class TestDbContextConfigurator
    {
        public static void Configure(DbContextOptionsBuilder<AppDbContext> builder)
            => builder.UseInMemoryDatabase(databaseName: "DataUpdaterTestDb");
    }
}
