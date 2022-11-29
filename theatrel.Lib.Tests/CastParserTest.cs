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

        [Theory]
        [InlineData("<p>При участии <a href=\"/company/academia/yulia_suleimanova/\">Юлии&nbsp;Сулеймановой</a> (сопрано) и <a href=\"/company/academia/trofimov/\">Александра&nbsp;Трофимова</a> (тенор)<br />Солист и дирижер&nbsp;&ndash;&nbsp;<a href=\"/company/conductors/lorenz_nasturica1/\">Лоренц Настурика-Гершовичи</a>&nbsp;(скрипка)</p>")]
        public async void TestTextParse(string text)
        {
            var factory = DIContainerHolder.Resolve<Func<Theatre, IPerformanceCastParser>>();
            var parser = factory(Theatre.Mariinsky);

            await parser.ParseText(text, CancellationToken.None);
        }
    }
}
