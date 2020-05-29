using System.Threading.Tasks;
using theatrel.Interfaces.Parsers;
using Xunit;

namespace theatrel.Tests
{
    public class TicketsParserTests
    {
        [Theory]
        [InlineData(@"..\..\..\TestData\p_theatre", 2100)]
        public async Task CheckMinPrice(string file, int expected)
        {
            string text = System.IO.File.ReadAllText(file);

            var parser = DIContainerHolder.Resolve<ITicketsParser>();
            var tickets = await parser.Parse(text);
            Assert.True(tickets.GetMinPrice() == expected);
        }
    }
}
