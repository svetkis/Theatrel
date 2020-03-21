using System.Linq;
using theatrel.Interfaces;
using theatrel.Interfaces.Parsers;
using Xunit;

namespace theatrel.Tests
{
    public class PerformanceParserTests
    {
        [Theory]
        [InlineData(@"..\..\..\TestData\MariinskyPlayBill032020.html", "Лебединое озеро", "Балет")]
        [InlineData(@"..\..\..\TestData\MariinskyPlayBill032020.html", "Садко", "Опера")]
        [InlineData(@"..\..\..\TestData\MariinskyPlayBill032020.html", "Фортепианные квинтеты. Брух. Шостакович", "Концерт")]
        public void CheckPerfomanceTypes(string file, string name, string expected)
        {
            string text = System.IO.File.ReadAllText(file);

            var parser = DIContainerHolder.Resolve<IPlayBillParser>();

            IPerformanceData[] perfomances = parser.Parse(text).GetAwaiter().GetResult();
            foreach (var perfomance in perfomances.Where(p => p.Name == name))
                Assert.True(perfomance.Type == expected);
        }
    }
}
