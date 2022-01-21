using System;
using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.TimeZoneService;

public interface ITimeZoneService : IDISingleton
{
    TimeZoneInfo TimeZone { get; set; }
    DateTime GetLocalTime(DateTime dateTime);
}