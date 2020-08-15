using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces;
using theatrel.Interfaces.Parsers;
using Xunit;

namespace theatrel.Lib.Tests
{
    public class PerformanceParserTests
    {
        [Theory]
        [InlineData(@"..\..\..\TestData\MariinskyPlayBill032020", "Лебединое озеро", "Балет")]
        [InlineData(@"..\..\..\TestData\MariinskyPlayBill032020", "Садко", "Опера")]
        [InlineData(@"..\..\..\TestData\MariinskyPlayBill032020", "Фортепианные квинтеты. Брух. Шостакович", "Концерт")]
        public async Task CheckPerformanceTypes(string file, string name, string expected)
        {
            string text = await System.IO.File.ReadAllTextAsync(file);

            var parser = DIContainerHolder.Resolve<IPlayBillParser>();

            IPerformanceData[] performances = await parser.Parse(text, CancellationToken.None);
            foreach (var performance in performances.Where(p => p.Name == name))
                Assert.Equal(expected, performance.Type);
        }

        [Theory]
        [InlineData(@"..\..\..\TestData\MariinskyPlayBill032020")]
        public async Task CheckCancellation(string file)
        {
            string text = await System.IO.File.ReadAllTextAsync(file);

            var parser = DIContainerHolder.Resolve<IPlayBillParser>();

            bool wasCanceled = false;
            try
            {
                IPerformanceData[] performances = await parser.Parse(text, new CancellationTokenSource(15).Token);
            }
            catch (TaskCanceledException ex)
            {
                wasCanceled = true;
            }

            Assert.True(wasCanceled);
        }
    }
}
