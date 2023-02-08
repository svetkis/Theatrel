using System;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Tickets;
using theatrel.Lib.Enums;
using System.Collections.Generic;

namespace theatrel.Lib;

public class DIFactoryOptions
{
    public IDictionary<Theatre, Type> PlaybillParserTypes { get; } = new Dictionary<Theatre, Type>();
    public IDictionary<Theatre, Type> PerformanceParserTypes { get; } = new Dictionary<Theatre, Type>();
    public IDictionary<Theatre, Type> CastParserTypes { get; } = new Dictionary<Theatre, Type>();
    public IDictionary<Theatre, Type> TicketsParserTypes { get; } = new Dictionary<Theatre, Type>();

    public void RegisterPlaybillParser<T>(Theatre type) where T : IPlaybillParser
    {
        PlaybillParserTypes.Add(type, typeof(T));
    }

    public void RegisterPerformanceParser<T>(Theatre type) where T : IPerformanceParser
    {
        PerformanceParserTypes.Add(type, typeof(T));
    }

    public void RegisterPerformanceCastParser<T>(Theatre type) where T : IPerformanceCastParser
    {
        CastParserTypes.Add(type, typeof(T));
    }

    public void RegisterTicketsParser<T>(Theatre type) where T : ITicketsParser
    {
        TicketsParserTypes.Add(type, typeof(T));
    }
}
