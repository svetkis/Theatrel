using Autofac;
using Autofac.Core;
using System;
using theatrel.DataAccess;
using theatrel.DataUpdater;
using theatrel.Lib;
using theatrel.Subscriptions;
using theatrel.TLBot;

namespace theatrel.ConsoleTest
{
    public static class Bootstrapper
    {
        public static ILifetimeScope RootScope;
        public static void Start()
        {
            if (RootScope != null)
                return;

            ContainerBuilder builder = new ContainerBuilder();

            builder.RegisterModule<TheatrelLibModule>();
            builder.RegisterModule<TlBotModule>();
            builder.RegisterModule<TheatrelDataAccessModule>();
            builder.RegisterModule<DataUpdaterModule>();
            builder.RegisterModule<SubscriptionModule>();

            RootScope = builder.Build();
        }

        public static void Stop()
        {
            RootScope?.Dispose();
            RootScope = null;
        }

        public static T Resolve<T>()
        {
            if (RootScope == null)
                throw new Exception("Bootstrapper hasn't been started!");

            return RootScope.Resolve<T>(new Parameter[0]);
        }

        public static T Resolve<T>(Parameter[] parameters)
        {
            if (RootScope == null)
                throw new Exception("Bootstrapper hasn't been started!");

            return RootScope.Resolve<T>(parameters);
        }
    }
}