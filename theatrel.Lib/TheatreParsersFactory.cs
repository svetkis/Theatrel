using System;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Tickets;
using theatrel.Lib.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace theatrel.Lib;

public class TheatreParsersFactory
{
    private readonly IServiceProvider _provider;
    private readonly DIFactoryOptions _options;

    public TheatreParsersFactory(
        IServiceProvider provider,
        IOptions<DIFactoryOptions> options)
    {
        _provider = provider;
        _options = options.Value;
    }

    public IPlaybillParser ResolvePlaybillParser(Theatre theatre)
    {
        if (_options.PlaybillParserTypes.TryGetValue(theatre, out var type))
        {
            return (IPlaybillParser)_provider.GetRequiredService(type);
        }

        throw new ArgumentOutOfRangeException(nameof(theatre));
    }

    public IPerformanceParser ResolvePerformanceParser(Theatre theatre)
    {
        if (_options.PerformanceParserTypes.TryGetValue(theatre, out var type))
        {
            return (IPerformanceParser)_provider.GetRequiredService(type);
        }

        throw new ArgumentOutOfRangeException(nameof(theatre));
    }

    public IPerformanceCastParser ResolvePerformanceCastParser(Theatre theatre)
    {
        if (_options.CastParserTypes.TryGetValue(theatre, out var type))
        {
            return (IPerformanceCastParser)_provider.GetRequiredService(type);
        }

        throw new ArgumentOutOfRangeException(nameof(theatre));
    }

    public ITicketsParser ResolveTicketsParserParser(Theatre theatre)
    {
        if (_options.TicketsParserTypes.TryGetValue(theatre, out var type))
        {
            return (ITicketsParser)_provider.GetRequiredService(type);
        }

        throw new ArgumentOutOfRangeException(nameof(theatre));
    }
}
