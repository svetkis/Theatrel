using System.Diagnostics;

namespace theatrel.Common;

public static class MemoryHelper
{
    private static readonly Process CurrentProcess = Process.GetCurrentProcess();

    public static void LogMemoryUsage()
    {
        //CurrentProcess.Refresh();
        //Console.WriteLine($"Memory usage is {CurrentProcess.WorkingSet64 / 1048576}");
    }

    public static void Collect(bool compactLoh)
    {
        /*var before = GC.GetTotalMemory(false) / 1048576;

        if (compactLoh)
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;*/

#pragma warning disable S1215 // "GC.Collect" should not be called
        //GC.Collect();
#pragma warning restore S1215 // "GC.Collect" should not be called

        //var after = GC.GetTotalMemory(true) / 1048576;

        //Console.WriteLine();
        //Console.WriteLine($"Total allocated before collection:{before} after collection: {after:N0}");
        //LogMemoryUsage();
    }
}