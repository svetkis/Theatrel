using theatrel.Lib.Enums;
using theatrel.Lib.MariinskyParsers;
using theatrel.Lib.MihailovkyParsers;
using theatrel.Interfaces.EncodingService;
using Microsoft.Extensions.DependencyInjection;
using theatrel.Lib.Interfaces;
using Microsoft.Extensions.DependencyInjection.Extensions;
using theatrel.Interfaces.Playbill;
using theatrel.Lib.Playbill;
using theatrel.Interfaces.TimeZoneService;

namespace theatrel.Lib;

public static class ServicesExtention
{
    public static IServiceCollection AddTheatrelLib(this IServiceCollection services)
    {
        services.AddTransient<IDescriptionService, DescriptionService.DescriptionService>();
        services.AddTransient<IEncodingService, EncodingServices.EncodingService>();

        services.AddTransient<IPlayBillDataResolver, PlayBillResolver>();

        services.AddSingleton<ITimeZoneService, TimeZoneService.TimeZoneService>();

        services.TryAddTransient<TheatreParsersFactory>();

        services.TryAddTransient<MariinskyPlaybillParser>();
        services.TryAddTransient<MihailovskyPlaybillParser>();

        services.TryAddTransient<MariinskyPerformanceParser>();
        services.TryAddTransient<MihailovskyPerformanceParser>();

        services.TryAddTransient<MariinskyTicketsBlockParser>();
        services.TryAddTransient<MihailovskyTicketsBlockParser>();

        services.TryAddTransient<MariinskyCastParser>();
        services.TryAddTransient<MihailovskyCastParser>();

        services.Configure<DIFactoryOptions>(options => 
        {
            options.RegisterPlaybillParser<MariinskyPlaybillParser>(Theatre.Mariinsky);
            options.RegisterPlaybillParser<MariinskyPlaybillParser>(Theatre.Mikhailovsky);

            options.RegisterPerformanceParser<MariinskyPerformanceParser>(Theatre.Mariinsky);
            options.RegisterPerformanceParser<MihailovskyPerformanceParser>(Theatre.Mikhailovsky);

            options.RegisterTicketsParser<MariinskyTicketsBlockParser>(Theatre.Mariinsky);
            options.RegisterTicketsParser<MihailovskyTicketsBlockParser>(Theatre.Mikhailovsky);

            options.RegisterPerformanceCastParser<MariinskyCastParser>(Theatre.Mariinsky);
            options.RegisterPerformanceCastParser<MihailovskyCastParser>(Theatre.Mikhailovsky);
        });

        return services;
    }
}
