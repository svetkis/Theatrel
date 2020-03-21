using Autofac;
using System.Reflection;
using theatrel.Interfaces;

namespace theatrel.TLBot
{
    public class TlBotModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            Assembly[] assemblies = { Assembly.GetExecutingAssembly() };

            builder.RegisterAssemblyTypes(assemblies)
               .Where(t => typeof(IDIRegistrableService).IsAssignableFrom(t))
               .SingleInstance()
               .AsImplementedInterfaces();
        }
    }
}
