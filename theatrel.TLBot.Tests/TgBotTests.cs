using Autofac;
using Moq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Playbill;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Tests.Settings;
using Xunit;
using Xunit.Abstractions;

namespace theatrel.TLBot.Tests
{
    public class TgBotTests : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;
        private readonly ITestOutputHelper _output;

        public TgBotTests(DatabaseFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
            Trace.Listeners.Add(new Trace2TestOutputListener(output));
        }

        [Theory]
        [InlineData(1, 5, new[] { DayOfWeek.Monday }, "�������", "������", "������", "���!", "���", "�������", "���", "�����������", "�������", "����������� �� �������� ����")]
        [InlineData(2, 5, new[] { DayOfWeek.Monday }, "�������", "������", "������", "������� ������ �����", "���", "�������", "���", "�����������", "�������")]
        [InlineData(3, 4, new[] { DayOfWeek.Saturday }, "�������", "������", "������", "�������", "�������")]
        [InlineData(4, 6, new[] { DayOfWeek.Sunday }, "�����", "Hi", "����", "��", "�����")]
        [InlineData(5, 7, new[] { DayOfWeek.Friday }, "�����", "������ ����!", "����", "5", "�����")]
        [InlineData(6, 5, new[] { DayOfWeek.Friday }, "�����", "������ ����!", "����", "������!", "���", "5", "�����")]
        public async Task DialogTest(long chatId, int month, DayOfWeek[] dayOfWeeks, string performanceType, params string[] commands)
        {
            IPerformanceFilter filter = null;

            var playBillResolverMock = new Mock<IPlayBillDataResolver>();
            playBillResolverMock.Setup(h => h.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Callback<IPerformanceFilter, CancellationToken>((filterResult, cToken) =>
                {
                    filter = filterResult;
                }).Returns(() => Task.FromResult(new IPerformanceData[0]));

            bool sent = false;
            var tgBotServiceMock = new Mock<ITgBotService>();
            tgBotServiceMock.Setup(x => x.Start(CancellationToken.None)).Verifiable();
            tgBotServiceMock.Setup(x => x.SendMessageAsync(It.IsAny<long>(), It.IsAny<ITgOutboundMessage>()))
                .Callback(() => { sent = true; });

            await using ILifetimeScope scope = _fixture.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(playBillResolverMock.Object).As<IPlayBillDataResolver>().AsImplementedInterfaces();
                builder.RegisterModule<TlBotModule>();
            });

            var tgProcessor = scope.Resolve<ITgBotProcessor>();

            //test
            tgProcessor.Start(tgBotServiceMock.Object, CancellationToken.None);

            foreach (var cmd in commands)
            {
                tgBotServiceMock.Raise(x => x.OnMessage += null, null,
                    Mock.Of<ITgInboundMessage>(m => m.Message == cmd && m.ChatId == chatId));

                while (!sent)
                    await Task.Delay(10);

                sent = false;
            }

            playBillResolverMock.Verify(lw => lw.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce());

            Assert.NotNull(filter);
            _output.WriteLine($"{string.Join(" ", filter.DaysOfWeek.OrderBy(d => d))}");
            _output.WriteLine($"{string.Join(" ", dayOfWeeks.OrderBy(d => d))}");
            Assert.True(filter.DaysOfWeek.OrderBy(d => d).SequenceEqual(dayOfWeeks.OrderBy(d => d)));
            Assert.Equal(month, filter.StartDate.Month);
            Assert.Equal(performanceType.ToLower(), filter.PerformanceTypes.First().ToLower());

            tgBotServiceMock.Verify(x => x.SendMessageAsync(It.IsAny<long>(), It.IsAny<ITgOutboundMessage>()), Times.AtLeastOnce);

            //second dialog after first
            foreach (var cmd in commands)
            {
                tgBotServiceMock.Raise(x => x.OnMessage += null, null,
                    Mock.Of<ITgInboundMessage>(m => m.Message == cmd && m.ChatId == 1));

                while (!sent)
                    await Task.Delay(10);

                sent = false;
            }

            tgProcessor.Stop();

            Assert.NotNull(filter);
            Assert.True(filter.DaysOfWeek.OrderBy(d => d).SequenceEqual(dayOfWeeks.OrderBy(d => d)));
            Assert.Equal(month, filter.StartDate.Month);
            Assert.Equal(performanceType.ToLower(), filter.PerformanceTypes.First().ToLower());
        }
    }
}
