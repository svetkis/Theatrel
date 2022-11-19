using System;
using System.Threading;
using theatrel.Interfaces.Cast;
using theatrel.Lib.Enums;
using Xunit;

namespace theatrel.Lib.Tests
{
    public class CastParserTest
    {
        [Theory]
        [InlineData("https://www.mariinsky.ru/playbill/playbill/2022/11/20/8_1600/")]
        public async void Test(string url)
        {
            var factory = DIContainerHolder.Resolve<Func<Theatre, IPerformanceCastParser>>();
            var parser = factory(Theatre.Mariinsky);

            await parser.ParseFromUrl(url, false, CancellationToken.None);
        }
    }
}
