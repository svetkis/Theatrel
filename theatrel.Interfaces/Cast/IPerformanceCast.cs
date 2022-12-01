using System.Collections.Generic;
using theatrel.Common.Enums;

namespace theatrel.Interfaces.Cast;

public interface IPerformanceCast
{
    CastState State { get; }

    IDictionary<string, IList<IActor>> Cast { get; set; }
}