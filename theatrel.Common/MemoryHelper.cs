using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime;

namespace theatrel.Common;

public class MemoryHelper
{
    public static void LogMemoryUsage()
    {
        Process currentProcess = Process.GetCurrentProcess();
        currentProcess.Refresh();
        Trace.TraceInformation($"Memory usage is {(currentProcess.WorkingSet64 / 1048576).ToString(CultureInfo.InvariantCulture)}");
    }

    public static void Collect(bool compactLoh)
    {
        Console.WriteLine();
        Console.WriteLine("Total allocated before collection: {0:N0}", GC.GetTotalMemory(false));

        if (compactLoh)
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

        GC.Collect();

        Console.WriteLine("Total allocated after collection: {0:N0}", GC.GetTotalMemory(true));
    }
}