using Autofac;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.TgBot;
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
        }

        [Theory]
        [InlineData(1, 5, new[] { DayOfWeek.Monday }, "�������", true, "/search", "�����", "������", "���!", "���", "�������", "���", "�����������", "�������", "����������� �� ����� ��������� � ��������� ������� � �������")]
        [InlineData(2, 5, new[] { DayOfWeek.Monday }, "�������", false, "������", "�����", "������", "������� ������ �����", "���", "�������", "���", "�����������", "�������", "�������, �� ����")]
        [InlineData(3, 4, new[] { DayOfWeek.Saturday }, "�������", false, "������", "�����", "������", "�������", "�������", "�������, �� ����")]
        [InlineData(4, 6, new[] { DayOfWeek.Sunday }, "�����", false, "Hi", "1", "����", "��", "�����", "�������, �� ����")]
        [InlineData(5, 7, new[] { DayOfWeek.Friday }, "�����", false, "������ ����!", "�����", "����", "5", "�����", "�������, �� ����")]
        [InlineData(6, 5, new[] { DayOfWeek.Friday }, "�����", false, "������ ����!", "��������-1", "����", "/search", "��������-2", "���", "5", "�����", "�������, �� ����")]
        public async Task DialogTest(long chatId, int month, DayOfWeek[] dayOfWeeks, string performanceType, bool subscribed, params string[] commands)
        {
            foreach (var entity in _fixture.Db.Subscriptions.AsNoTracking())
            {
                _fixture.Db.Subscriptions.Remove(entity);
            }

            await _fixture.Db.SaveChangesAsync();

            IChatDataInfo chatData = null;

            var performanceFilterMock = new Mock<IPerformanceFilter>();

            var filterServiceMock = new Mock<IFilterService>();
            filterServiceMock.Setup(h => h.IsDataSuitable(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<IPerformanceFilter>()))
                .Returns(() => true);

            filterServiceMock.Setup(h => h.GetFilter(It.IsAny<IChatDataInfo>()))
                .Callback<IChatDataInfo>(chatInfo =>
                {
                    chatData = chatInfo;
                }).Returns(() => performanceFilterMock.Object);

            bool sent = false;
            var tgBotServiceMock = new Mock<ITgBotService>();
            tgBotServiceMock.Setup(x => x.Start(CancellationToken.None)).Verifiable();
            tgBotServiceMock.Setup(x => x.SendMessageAsync(It.IsAny<long>(), It.IsAny<ITgCommandResponse>(), It.IsAny<CancellationToken>()))
                .Callback(() => { sent = true; });

            await using ILifetimeScope scope = _fixture.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(filterServiceMock.Object).As<IFilterService>().AsImplementedInterfaces();
                builder.RegisterModule<TlBotModule>();
            });

            var tgProcessor = scope.Resolve<ITgBotProcessor>();

            //test
            tgProcessor.Start(tgBotServiceMock.Object, CancellationToken.None);

            foreach (var cmd in commands)
            {
                _output.WriteLine($"Send message: {cmd}");
                tgBotServiceMock.Raise(x => x.OnMessage += null, null,
                    Mock.Of<ITgInboundMessage>(m => m.Message == cmd && m.ChatId == chatId));

                while (!sent && cmd != commands.Last())
                {
                    _output.WriteLine("Waiting for response message");
                    await Task.Delay(10);
                }

                sent = false;
            }

            Assert.NotNull(chatData);
            _output.WriteLine($"{string.Join(" ", chatData.Days.OrderBy(d => d))}");
            _output.WriteLine($"{string.Join(" ", dayOfWeeks.OrderBy(d => d))}");
            Assert.True(chatData.Days.OrderBy(d => d).SequenceEqual(dayOfWeeks.OrderBy(d => d)));
            Assert.Equal(month, chatData.When.Month);
            Assert.Equal(performanceType.ToLower(), chatData.Types.First().ToLower());

            tgBotServiceMock.Verify(x =>
                x.SendMessageAsync(It.IsAny<long>(), It.IsAny<ITgCommandResponse>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);

            //second dialog after first
            foreach (var cmd in commands)
            {
                _output.WriteLine($"Send message: {cmd}");
                tgBotServiceMock.Raise(x => x.OnMessage += null, null,
                    Mock.Of<ITgInboundMessage>(m => m.Message == cmd && m.ChatId == 1));

                while (!sent && cmd != commands.Last())
                {
                    _output.WriteLine("Waiting for response message");
                    await Task.Delay(10);
                }

                sent = false;
            }

            tgProcessor.Stop();

            Assert.Equal(subscribed, _fixture.Db.Subscriptions.Any());

            Assert.NotNull(chatData);
            Assert.True(chatData.Days.OrderBy(d => d).SequenceEqual(dayOfWeeks.OrderBy(d => d)));
            Assert.Equal(month, chatData.When.Month);
            Assert.Equal(performanceType.ToLower(), chatData.Types.First().ToLower());
        }
    }
}
