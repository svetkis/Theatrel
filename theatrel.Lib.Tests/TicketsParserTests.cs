using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Tickets;
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

            var parser = DIContainerHolder.Resolve<ITicketsParser>();

            var tickets = await parser.Parse(text, CancellationToken.None);
            Assert.Equal(expected, tickets.GetMinPrice());
        }

        [Theory]
        [InlineData(@"..\..\..\TestData\p_theatre")]
        public async Task CheckCancellation(string file)
        {
            string text = await System.IO.File.ReadAllTextAsync(file);

            var parser = DIContainerHolder.Resolve<ITicketsParser>();

            await Assert.ThrowsAsync<TaskCanceledException>(async () => await parser.Parse(text, new CancellationTokenSource(10).Token));
        }
    }
}
