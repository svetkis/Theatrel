﻿using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using theatrel.Interfaces.Filters;

namespace theatrel.DataAccess.Structures.Entities;

public class PerformanceFilterEntity : IPerformanceFilter
{
    public int Id { get; set; }

    public string PerformanceName { get; set; }

    public string Actor { get; set; }

    [NotMapped]
    public DayOfWeek[] DaysOfWeek
    {
        get => string.IsNullOrEmpty(DbDaysOfWeek) ? null : DbDaysOfWeek?.Split(',').Select(d => (DayOfWeek)int.Parse(d)).ToArray();
        set => DbDaysOfWeek = value != null
            ? string.Join(",", value.OrderBy(d => d).Select(d => ((int)d).ToString()))
            : null;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public string DbDaysOfWeek { get; set; }

    [NotMapped]
    public string[] PerformanceTypes
    {
        get => string.IsNullOrEmpty(DbPerformanceTypes) ? null : DbPerformanceTypes.Split(',');
        set => DbPerformanceTypes = value != null
            ? string.Join(",", value.OrderBy(s => s))
            : null;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public string DbPerformanceTypes { get; set; }

    private string IntArrayToString(int[] array)
    {
        return array != null
            ? string.Join(",", array.OrderBy(s => s))
            : null;
    }

    private int[] StringToIntArray(string source)
    {
        return string.IsNullOrEmpty(source)
            ? null
            : source.Split(',').Select(x =>
            {
                bool parsed = int.TryParse(x, out int result);
                return parsed ? result : 0;
            }).ToArray();
    }

    [NotMapped]
    public int[] LocationIds
    {
        get => StringToIntArray(DbLocations);
        set => DbLocations = IntArrayToString(value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public string DbLocations { get; set; }


    [NotMapped]
    public int[] TheatreIds
    {
        get => StringToIntArray(DbTheatres);
        set => DbTheatres = IntArrayToString(value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public string DbTheatres { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public int PlaybillId { get; set; } = -1;
}