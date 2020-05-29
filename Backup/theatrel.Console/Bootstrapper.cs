using Autofac;
using Autofac.Core;
using System;
using theatrel.Lib;
using theatrel.TLBot;

namespace theatrel.Console
{
    public static class Bootstrapper
    {
        private static ILifetimeScope _rootScope;

        public static void Start()
        {
            if (_rootScope != null)
                return;

            ContainerBuilder builder = new ContainerBuilder();

            builder.RegisterModule<TheatrelLibModule>();
            builder.RegisterModule<TlBotModule>();

            _rootScope = builder.Build();
        }

        public static void Stop()
        {
            _rootScope?.Dispose();
            _rootScope = null;
        }

        public static T Resolve<T>()
        {
            if (_rootScope == null)
                throw new Exception("Bootstrapper hasn't been started!");

            return _rootScope.Resolve<T>(new Parameter[0]);
        }

        public static T Resolve<T>(Parameter[] parameters)
        {
            if (_rootScope == null)
                throw new Exception("Bootstrapper hasn't been started!");

            return _rootScope.Resolve<T>(parameters);
        }
    }
}
