using System.Reflection;
using Autofac;
using theatrel.Interfaces;

namespace theatrel.DataUpdater
{
    public class DataUpdaterModule : Autofac.Module
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
}
