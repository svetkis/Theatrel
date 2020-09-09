using System.Diagnostics;
using System.Globalization;

namespace theatrel.Common
{
    public class MemoryHelper
    {
        public static void LogMemoryUsage()
        {
            Process currentProcess = Process.GetCurrentProcess();
            currentProcess.Refresh();
            Trace.TraceInformation($"Memory usage is {(currentProcess.WorkingSet64 / 1048576).ToString(CultureInfo.InvariantCulture)}");
        }
    }
}
