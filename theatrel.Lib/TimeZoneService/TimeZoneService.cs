using System;
using theatrel.Interfaces.TimeZoneService;

namespace theatrel.Lib.TimeZoneService
{
    internal class TimeZoneService : ITimeZoneService
    {
        public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;
    }
}
