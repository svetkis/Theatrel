using System;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Tickets;
using theatrel.Lib.Enums;
using Xunit;

namespace theatrel.Lib.Tests
{
    public class TicketsParserTests
    {
        [Theory]
        [InlineData(@"..\..\..\TestData\p_theatre", 2100)]
        public async Task CheckMinPrice(string file, int expected)
        {
            string text = await System.IO.File.ReadAllTextAsync(file);

            var ticketsParserFactory = DIContainerHolder.Resolve<Func<Theatre, ITicketsParser>>();
            var parser = ticketsParserFactory(Theatre.Mariinsky);

            var tickets = await parser.Parse(text, CancellationToken.None);
            Assert.Equal(expected, tickets.GetMinPrice());
        }

        [Theory]
        [InlineData(@"..\..\..\TestData\p_theatre")]
        public async Task CheckCancellation(string file)
        {
            string text = await System.IO.File.ReadAllTextAsync(file);

            var ticketsParserFactory = DIContainerHolder.Resolve<Func<Theatre, ITicketsParser>>();
            var parser = ticketsParserFactory(Theatre.Mariinsky);

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await parser.Parse(text, new CancellationTokenSource(10).Token));
        }
    }
}
