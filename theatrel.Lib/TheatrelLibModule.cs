using System;
using Autofac;
using System.Reflection;
using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Tickets;
using theatrel.Lib.Enums;
using theatrel.Lib.MariinskyParsers;
using theatrel.Lib.MihailovkyParsers;

namespace theatrel.Lib
{
    public class TheatrelLibModule : Autofac.Module
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

            builder.Register<IPerformanceCastParser>((c, p) =>
                {
                    var type = p.TypedAs<Theatre>();
                    return type switch
                    {
                        Theatre.Mariinsky => new MariinskyCastParser(),
                        Theatre.Mikhailovsky => new MihailovskyCastParser(),
                        _ => throw new ArgumentException("Unknown theatre")
                    };
                })
                .As<IPerformanceCastParser>();

            builder.Register<IPerformanceParser>((c, p) =>
                {
                    var type = p.TypedAs<Theatre>();
                    return type switch
                    {
                        Theatre.Mariinsky => new MariinskyPerformanceParser(),
                        Theatre.Mikhailovsky => new MihailovskyPerformanceParser(),
                        _ => throw new ArgumentException("Unknown theatre")
                    };
                })
                .As<IPerformanceParser>();

            builder.Register<ITicketsParser>((c, p) =>
                {
                    var type = p.TypedAs<Theatre>();
                    return type switch
                    {
                        Theatre.Mariinsky => new MariinskyTicketsBlockParser(),
                        Theatre.Mikhailovsky => new MihailovskyTicketsBlockParser(),
                        _ => throw new ArgumentException("Unknown theatre")
                    };
                })
                .As<ITicketsParser>();

            builder.Register<ITicketParser>((c, p) =>
                {
                    var type = p.TypedAs<Theatre>();
                    return type switch
                    {
                        Theatre.Mariinsky => new MariinskyTicketParser(),
                        Theatre.Mikhailovsky => new MihailovskyTicketParser(),
                        _ => throw new ArgumentException("Unknown theatre")
                    };
                })
                .As<ITicketParser>();

            builder.Register<IPlaybillParser>((c, p) =>
                {
                    var type = p.TypedAs<Theatre>();
                    return type switch
                    {
                        Theatre.Mariinsky => new MariinskyPlaybillParser(),
                        Theatre.Mikhailovsky => new MihailovskyPlaybillParser(),
                        _ => throw new ArgumentException("Unknown theatre")
                    };
                })
                .As<IPlaybillParser>();
        }
    }
}
