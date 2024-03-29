﻿using System.Collections.Generic;
using theatrel.Common.Enums;
using theatrel.Interfaces.Cast;

namespace theatrel.Lib.Cast;

internal class PerformanceCast : IPerformanceCast
{
    public CastState State { get; set; }
    public IDictionary<string, IList<IActor>> Cast { get; set; } = new Dictionary<string, IList<IActor>>();
}