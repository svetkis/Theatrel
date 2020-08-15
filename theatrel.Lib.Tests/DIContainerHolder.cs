using System;
using Autofac;
using Autofac.Core;

namespace theatrel.Lib.Tests
{
    public static class DIContainerHolder
    {
        private static readonly ILifetimeScope RootScope;

        static DIContainerHolder()
        {
            ContainerBuilder containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule<DataResolverTestModule>();

            RootScope = containerBuilder.Build();
        }

        public static T Resolve<T>()
        {
            if (RootScope == null)
                throw new Exception("Bootstrapper hasn't been started!");

            return RootScope.Resolve<T>(new Parameter[0]);
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
