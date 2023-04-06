using System;
using Autofac;
using System.Reflection;
using theatrel.Interfaces.Autofac;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.Helpers;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Tickets;
using theatrel.Lib.Enums;
using theatrel.Lib.MariinskyParsers;
using theatrel.Lib.MihailovkyParsers;
using theatrel.Interfaces.EncodingService;

namespace theatrel.Lib;
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
                    Theatre.Mariinsky => new MariinskyCastParser(c.Resolve<IPageRequester>()),
                    Theatre.Mikhailovsky => new MihailovskyCastParser(c.Resolve<IPageRequester>()),
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
                    Theatre.Mariinsky => new MariinskyTicketsBlockParser(c.Resolve<IPageRequester>()),
                    Theatre.Mikhailovsky => new MihailovskyTicketsBlockParser(c.Resolve<IPageRequester>(), c.Resolve<IEncodingService>()),
                    _ => throw new ArgumentException("Unknown theatre")
                };
            })
            .As<ITicketsParser>();

        _ = builder.Register<IPlaybillParser>((c, p) =>
            {
                Theatre type = p.TypedAs<Theatre>();
                return type switch
                {
                    Theatre.Mariinsky => new MariinskyPlaybillParser(),
                    Theatre.Mikhailovsky => new MihailovskyPlaybillParser(c.Resolve<IPageRequester>()),
                    _ => throw new ArgumentException("Unknown theatre")
                };
            })
            .As<IPlaybillParser>();
    }
}