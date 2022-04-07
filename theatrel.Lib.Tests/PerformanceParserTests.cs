using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Parsers;
using theatrel.Interfaces.Playbill;
using theatrel.Lib.Enums;
using Xunit;

namespace theatrel.Lib.Tests;

public class PerformanceParserTests
{
    [Theory]
    [InlineData(@"..\..\..\TestData\MariinskyPlayBill032020", "Лебединое озеро", "Балет")]
    [InlineData(@"..\..\..\TestData\MariinskyPlayBill032020", "Садко", "Опера")]
    [InlineData(@"..\..\..\TestData\MariinskyPlayBill032020", "Фортепианные квинтеты. Брух. Шостакович", "Концерт")]
    public async Task CheckPerformanceTypes(string file, string name, string expected)
    {
        var text = await System.IO.File.ReadAllBytesAsync(file);

        var playbillParserFactory = DIContainerHolder.Resolve<Func<Theatre, IPlaybillParser>>();
        var parser = playbillParserFactory(Theatre.Mariinsky);

        var performanceParserFactory = DIContainerHolder.Resolve<Func<Theatre, IPerformanceParser>>();
        var performanceParser = performanceParserFactory(Theatre.Mariinsky);

        IPerformanceData[] performances = await parser.Parse(text, performanceParser, 0, 0, CancellationToken.None);
        foreach (var performance in performances.Where(p => p.Name == name))
            Assert.Equal(expected, performance.Type);
    }

    [Theory]
    [InlineData(12, 0, @"..\..\..\TestData\MariinskyPlayBill032020")]
    [InlineData(19, 30, @"..\..\..\TestData\MariinskiPB092020.txt")]
    public async Task CheckDateTime(int hour, int minute, string file)
    {
        var text = await System.IO.File.ReadAllBytesAsync(file);

        var playbillParserFactory = DIContainerHolder.Resolve<Func<Theatre, IPlaybillParser>>();
        var parser = playbillParserFactory(Theatre.Mariinsky);

        var performanceParserFactory = DIContainerHolder.Resolve<Func<Theatre, IPerformanceParser>>();
        var performanceParser = performanceParserFactory(Theatre.Mariinsky);

         var performances = await parser.Parse(text, performanceParser, 0, 0, CancellationToken.None);

        var timeZone = TimeZoneInfo.CreateCustomTimeZone("Moscow Time", new TimeSpan(03, 00, 00),
            "(GMT+03:00) Moscow Time", "Moscow Time");

        DateTime dt = TimeZoneInfo.ConvertTimeFromUtc(performances.OrderBy(p => p.DateTime).First().DateTime, timeZone);

        Assert.Equal(hour, dt.Hour);
        Assert.Equal(minute, dt.Minute);

        var exceptions = Record.Exception(() =>
        {
            foreach (var p in performances)
                TimeZoneInfo.ConvertTimeFromUtc(p.DateTime, timeZone);
        });

        Assert.Null(exceptions);
    }
}