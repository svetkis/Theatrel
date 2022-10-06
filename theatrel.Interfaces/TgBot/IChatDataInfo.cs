using System;
using System.Collections.Generic;

namespace theatrel.Interfaces.TgBot;

public enum DialogState
{
    DialogStarted,
    DialogReturned
}

public interface IChatDataInfo
{
    long UserId { get; set; }
    string Culture { get; set; }

    int CommandLine { get; set; }
    int CurrentStepId { get; set; }
    int PreviousStepId { get; set; }
    DateTime When { get; set; }
    IEnumerable<DayOfWeek> Days { get; set; }
    string[] Types { get; set; }
    int[] TheatreIds { get; set; }
    int[] LocationIds { get; set; }
    string PerformanceName { get; set; }
    string Actor { get; set; }
    DateTime LastMessage { get; set; }
    DialogState DialogState { get; set; }

    public string Info { get; set; }

    void Clear();
}