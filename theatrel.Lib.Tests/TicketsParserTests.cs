using theatrel.Interfaces.Parsers;
using Xunit;

namespace theatrel.Tests
{
    public class TicketsParserTests
    {
        [Theory]
        [InlineData(@"..\..\..\TestData\p_theatre.html", 2100)]
        public void CheckMinPrice(string file, int expected)
        {
            string text = System.IO.File.ReadAllText(file);

            var parser = DIContainerHolder.Resolve<ITicketsParser>();
            var tickets = parser.Parse(text).GetAwaiter().GetResult();
            Assert.True(tickets.GetMinPrice() == expected);
        }
    }
}
