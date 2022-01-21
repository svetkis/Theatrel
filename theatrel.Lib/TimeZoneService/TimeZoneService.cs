using System;
using theatrel.Interfaces.TimeZoneService;

namespace theatrel.Lib.TimeZoneService;

internal class TimeZoneService : ITimeZoneService
{
    public TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    public DateTime GetLocalTime(DateTime dateTime)
    {
        return dateTime.Kind == DateTimeKind.Utc
            ? TimeZoneInfo.ConvertTimeFromUtc(dateTime, TimeZone)
            : dateTime.AddHours(3);
    }
}