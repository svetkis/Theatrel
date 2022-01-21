using System;
using System.Diagnostics;

namespace theatrel.Common;

public class Trace2StdoutLogger : TraceListener
{
    public override void Write(string message)
    {
        Console.WriteLine();
        Console.Write($"{DateTime.UtcNow:MM/dd hh:mm:ss} ");
    }

    public override void WriteLine(string message)
    {
        Console.Write(message);
        Console.Write(" ");
    }
}