using Autofac;
using Autofac.Core;
using System;

namespace theatrel.Lib.Tests
{
    public static class DIContainerHolder
    {
        private static readonly ILifetimeScope RootScope;

        static DIContainerHolder()
        {
            ContainerBuilder containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule<DataResolverTestModule>();
            containerBuilder.RegisterModule<TheatrelLibModule>();

            RootScope = containerBuilder.Build();
        }

        public static T Resolve<T>()
        {
            if (RootScope == null)
                throw new Exception("Bootstrapper hasn't been started!");

            return RootScope.Resolve<T>(Array.Empty<Parameter>());
        }
    }

    public class DataResolverTestModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<TheatrelLibModule>();
            base.Load(builder);
        }
    }
}
