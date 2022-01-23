using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;

namespace theatrel.Common;

public class MemoryHelper
{
    public static void LogMemoryUsage()
    {
        using Process currentProcess = Process.GetCurrentProcess();
        currentProcess.Refresh();
        Console.WriteLine($"Memory usage is {currentProcess.WorkingSet64 / 1048576}");
    }

    public static void Collect(bool compactLoh)
    {
        var before = GC.GetTotalMemory(false) / 1048576;

        if (compactLoh)
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

        GC.Collect();

        var after = GC.GetTotalMemory(true) / 1048576;

        Console.WriteLine();
        Console.WriteLine($"Total allocated before collection:{before} after collection: {after:N0}");
        LogMemoryUsage();
    }
}