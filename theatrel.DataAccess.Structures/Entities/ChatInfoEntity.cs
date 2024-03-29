﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using theatrel.Interfaces.TgBot;

namespace theatrel.DataAccess.Structures.Entities;

public class ChatInfoEntity : IChatDataInfo
{
    [Key]
    public long UserId { get; set; }

    public string Culture { get; set; }

    public int CommandLine { get; set; }
    public int CurrentStepId { get; set; }
    public int PreviousStepId { get; set; }

    public string Info { get; set; }

    public DateTime When { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public IEnumerable<DayOfWeek> Days
    {
        get => DbDays?.Split(',').Select(d => (DayOfWeek)int.Parse(d));
        set => DbDays = value == null ? null : string.Join(",", value.Select(d => ((int)d).ToString()));
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public string DbDays { get; set; }

    [NotMapped]
    public string[] Types
    {
        get => GetTypes();
        set => DbTypes = value == null || !value.Any() ? null : string.Join(",", value);
    }

    private string[] GetTypes() => DbTypes?.Split(',').ToArray();

    [NotMapped]
    public int[] TheatreIds
    {
        get => GetTheatreIds();
        set => DbTheatres = value == null || !value.Any() ? null : string.Join(",", value);
    }

    private int[] GetTheatreIds() => DbTheatres?.Split(',').Select(x => int.Parse(x)).ToArray();

    [NotMapped]
    public int[] LocationIds
    {
        get => GetLocationIds();
        set => DbLocations = value == null || !value.Any() ? null : string.Join(",", value);
    }

    private int[] GetLocationIds() => DbLocations?.Split(',').Select(x => int.Parse(x)).ToArray();
    public string PerformanceName { get; set; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public string DbTypes { get; set; }

    public string DbLocations { get; set; }

    public string DbTheatres { get; set; }

    public string Actor { get; set; }

    public DateTime LastMessage { get; set; } = DateTime.UtcNow;
    public DialogState DialogState { get; set; }


    public void Clear()
    {
        PerformanceName = null;
        Actor = null;

        CommandLine = 0;

        CurrentStepId = 0;
        PreviousStepId = -1;

        Days = null;
        Types = null;
        DialogState = DialogState.DialogStarted;

        LastMessage = DateTime.UtcNow;
    }
}