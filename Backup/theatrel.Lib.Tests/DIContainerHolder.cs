using Autofac;
using Autofac.Core;
using System;
using theatrel.Lib;

namespace theatrel.Tests
{
    public static class DIContainerHolder
    {
        private static ILifetimeScope _rootScope;

        static DIContainerHolder()
        {
            ContainerBuilder containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule<DataResolverTestModule>();

            _rootScope = containerBuilder.Build();
        }

        public static T Resolve<T>()
        {
            if (_rootScope == null)
                throw new Exception("Bootstrapper hasn't been started!");

            return _rootScope.Resolve<T>(new Parameter[0]);
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
