using System.Diagnostics;
using Xunit.Abstractions;

namespace theatrel.TLBot.Tests
{
    internal class Trace2TestOutputListener : TraceListener
    {
        private readonly ITestOutputHelper _testOutput;

        public Trace2TestOutputListener(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }

        public override void Write(string message)
        {
        }

        public override void WriteLine(string message) => _testOutput.WriteLine(message);
    }
}
