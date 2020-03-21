using Autofac;
using System;
using theatrel.Lib;

namespace theatrel.TLBot.Tests
{
    public class DIContainerHolder
    {
        private static Lazy<DIContainerHolder> DIContainer = new Lazy<DIContainerHolder>(() => new DIContainerHolder());
        private ILifetimeScope _rootScope;

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
            builder.RegisterModule<TlBotModule>();

            base.Load(builder);
        }
    }
}
