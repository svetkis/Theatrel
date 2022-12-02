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
        [InlineData("https://www.mariinsky.ru/playbill/playbill/2022/12/7/2_1900/",
            "<p>При участии <a href=\"/company/ballet_mt_women/vishneva/\">Дианы&nbsp;Вишнёвой</a>, <a href=\"/company/ballet_mt_women/kondaurova/\">Екатерины&nbsp;Кондауровой</a>, <a href=\"/company/ballet_mt_women/ilyushkina/\">Марии Ильюшкиной</a>, <a href=\"/company/ballet_mt_men/askerov/\">Тимура&nbsp;Аскерова</a>, <a href=\"/company/ballet_mt_men/yermakov/\">Андрея&nbsp;Ермакова</a>,&nbsp;<a href=\"/company/ballet_mt_men/ivanchenko/\">Евгения&nbsp;Иванченко</a>, <a href=\"/company/ballet_mt_men/zverev/\">Константина&nbsp;Зверева</a></p>)]")]
        public async void Test(string url, string text)
        {
            var factory = DIContainerHolder.Resolve<Func<Theatre, IPerformanceCastParser>>();
            var parser = factory(Theatre.Mariinsky);

            await parser.ParseFromUrl(url, text, false, CancellationToken.None);
        }
    }
}
