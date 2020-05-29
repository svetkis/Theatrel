using System.Linq;
using System.Threading.Tasks;
using theatrel.Interfaces;
using theatrel.Interfaces.Parsers;
using Xunit;

namespace theatrel.Tests
{
    public class PerformanceParserTests
    {
        [Theory]
        [InlineData(@"..\..\..\TestData\MariinskyPlayBill032020", "Лебединое озеро", "Балет")]
        [InlineData(@"..\..\..\TestData\MariinskyPlayBill032020", "Садко", "Опера")]
        [InlineData(@"..\..\..\TestData\MariinskyPlayBill032020", "Фортепианные квинтеты. Брух. Шостакович", "Концерт")]
        public async Task CheckPerformanceTypes(string file, string name, string expected)
        {
            string text = System.IO.File.ReadAllText(file);

            var parser = DIContainerHolder.Resolve<IPlayBillParser>();

            IPerformanceData[] performances = await parser.Parse(text);
            foreach (var performance in performances.Where(p => p.Name == name))
                Assert.True(performance.Type == expected);
        }
    }
}
