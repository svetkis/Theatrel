using System;
using System.Threading;
using theatrel.Interfaces.Cast;
using theatrel.Lib.Enums;
using Xunit;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace theatrel.Lib.Tests
{
    public class CastParserTest
    {
        [Theory]
        [InlineData("https://www.mariinsky.ru/playbill/playbill/2022/12/13/2_1900/",
            "<p>Дирижер &ndash; <a href=\"/company/conductors/gergiev/\">Валерий&nbsp;Гергиев</a></p>")]
        public async void Test(string url, string text)
        {
            var factory = DIContainerHolder.Resolve<Func<Theatre, IPerformanceCastParser>>();
            var parser = factory(Theatre.Mariinsky);

            await parser.ParseFromUrl(url, text, false, CancellationToken.None);
        }
    }
}
