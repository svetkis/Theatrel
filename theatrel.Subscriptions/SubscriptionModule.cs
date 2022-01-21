using Autofac;
using System.Reflection;
using theatrel.Interfaces.Autofac;

namespace theatrel.Subscriptions;

public class SubscriptionModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        Assembly[] assemblies = { Assembly.GetExecutingAssembly() };

        builder.RegisterAssemblyTypes(assemblies)
            .Where(t => typeof(IDIRegistrable).IsAssignableFrom(t))
            .AsImplementedInterfaces();

        builder.RegisterAssemblyTypes(assemblies)
            .Where(t => typeof(IDISingleton).IsAssignableFrom(t))
            .SingleInstance()
            .AsImplementedInterfaces();
    }
}