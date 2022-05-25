using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace theatrel.ConsoleTest;

internal sealed class GarbageCollectorEventListener : EventListener
{
    //https://docs.microsoft.com/en-us/dotnet/framework/performance/garbage-collection-etw-events

    private const int GcKeyword = 0x0000001;
    private const int TypeKeyword = 0x0080000;
    private const int GcHeapAndTypeNamesKeyword = 0x1000000;

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        Console.WriteLine($"{eventSource.Guid} | {eventSource.Name}");

        // look for .NET Garbage Collection events
        if (eventSource.Name.Equals("Microsoft-Windows-DotNETRuntime"))
        {
            EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)(GcKeyword | GcHeapAndTypeNamesKeyword | TypeKeyword));
        }
    }

    // from https://blogs.msdn.microsoft.com/dotnet/2018/12/04/announcing-net-core-2-2/
    // Called whenever an event is written.
    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {

        switch (eventData.EventName)
        {
            case "GCHeapStats_V1":
                ProcessHeapStats(eventData);
                break;
            case "GCAllocationTick_V2":
                ProcessAllocationEvent(eventData);
                break;
            case "GCAllocationTick_V3":
                ProcessAllocationEvent(eventData);
                break;
        }
            
    }

    private void ProcessAllocationEvent(EventWrittenEventArgs eventData)
    {
        Trace.TraceInformation($"GS Event: {eventData.EventName} AllocatedMemory {(ulong)eventData.Payload[3]} {(string)eventData.Payload[5]}");
    }

    private void ProcessHeapStats(EventWrittenEventArgs eventData)
    {
        Trace.TraceInformation($"GS Event: {eventData.EventName}");

        Trace.TraceInformation($"Gen0 Size {(ulong)eventData.Payload[0]} Gen0 Promoted {(ulong)eventData.Payload[1]}");
        Trace.TraceInformation($"Gen1 Size {(ulong)eventData.Payload[2]} Gen1 Promoted {(ulong)eventData.Payload[3]}");
        Trace.TraceInformation($"Gen2 Size {(ulong)eventData.Payload[4]} Gen2 Survived {(ulong)eventData.Payload[5]}");
        Trace.TraceInformation($"LOH Size {(ulong)eventData.Payload[6]} LOH Survived {(ulong)eventData.Payload[7]}");
    }
}