using System;
using System.Diagnostics;
using System.Threading.Tasks;
using JetBrains.Profiler.Api;
using theatrel.Common;

namespace theatrel.ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Trace.Listeners.Add(new Trace2StdoutLogger());

            Bootstrapper.Start();

            GC.Collect();
            MemoryProfiler.GetSnapshot();

            await SubscriptionsTest.Test();

            GC.Collect();
            MemoryProfiler.GetSnapshot();

            await Task.Delay(1000);
            Bootstrapper.Stop();
        }
    }
}
