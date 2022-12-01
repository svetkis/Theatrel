using System.Collections.Generic;
using System.Linq;
using theatrel.Common.Enums;
using theatrel.Interfaces.Cast;

namespace theatrel.Lib.Cast;

internal class PerformanceCast : IPerformanceCast
{
    public CastState State => Cast.Any() ? CastState.Ok : CastState.CastIsNotSet;
    public IDictionary<string, IList<IActor>> Cast { get; set; }
}