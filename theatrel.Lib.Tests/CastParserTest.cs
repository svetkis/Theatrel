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
        [InlineData(
            "https://www.mariinsky.ru/playbill/playbill/2022/12/13/2_1900/",
            "<p>Дирижер &ndash; <a href=\"/company/conductors/gergiev/\">Валерий&nbsp;Гергиев</a></p>")]
        public async void Test(string url, string text)
        {
            var factory = DIContainerHolder.Resolve<Func<Theatre, IPerformanceCastParser>>();
            var parser = factory(Theatre.Mariinsky);

            var cast = await parser.ParseFromUrl(url, text, false, CancellationToken.None);
        }

        [Theory]
        [InlineData(
            "https://mikhailovsky.ru/afisha/performances/detail/1904249/",
            "<p class=\"f-ap\">Спартак — <a href=\"/theatre/company/principals_m/ivan_zaytsev/\"data-link=\"actor\">Иван&nbsp;Зайцев</a><br>Валерия — <a href=\"/theatre/company/first-soloists-f/valeria_zapasnikova/\" data-link=\"actor\">Валерия&nbsp;Запасникова</a><br>Красс — <a href=\"/theatre/company/soloists_m/batalov_mikhail/\" data-link=\"actor\">Михаил&nbsp;Баталов</a><br>Сабина — <a href=\"/theatre/company/principals_f/irina_perren/\" data-link=\"actor\">Ирина&nbsp;Перрен</a><br>Помпей — <a href=\"/theatre/company/guest/farukh_ruzimatov/\" data-link=\"actor\">Фарух&nbsp;Рузиматов</a></p>")]
        public async void TestMichailovsky(string url, string text)
        {
            var factory = DIContainerHolder.Resolve<Func<Theatre, IPerformanceCastParser>>();
            var parser = factory(Theatre.Mikhailovsky);

            var cast = await parser.ParseFromUrl(url, text, false, CancellationToken.None);
        }
    }
}
