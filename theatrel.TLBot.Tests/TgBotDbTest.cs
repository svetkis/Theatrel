using Autofac;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Filters;
using theatrel.Interfaces.Playbill;
using theatrel.TLBot.Interfaces;
using theatrel.TLBot.Tests.Settings;
using Xunit;

namespace theatrel.TLBot.Tests
{
    public class TgBotDbTest : IClassFixture<DatabaseFixture>
    {
        private readonly DatabaseFixture _fixture;
        public TgBotDbTest(DatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task ParallelWritingToDb()
        {
            var tgBotServiceMock = new Mock<ITgBotService>();
            tgBotServiceMock.Setup(x => x.Start(CancellationToken.None)).Verifiable();
            tgBotServiceMock.Setup(x => x.SendMessageAsync(It.IsAny<long>(), It.IsAny<ITgOutboundMessage>())).Verifiable();

            var playBillResolverMock = new Mock<IPlayBillDataResolver>();
            playBillResolverMock.Setup(h => h.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new IPerformanceData[0]));

            await using ILifetimeScope scope = _fixture.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(playBillResolverMock.Object).As<IPlayBillDataResolver>().AsImplementedInterfaces();
                builder.RegisterModule<TlBotModule>();
            });

            var tgProcessor = scope.Resolve<ITgBotProcessor>();

            //test
            tgProcessor.Start(tgBotServiceMock.Object, CancellationToken.None);

            var random = new Random();
            var ids4Test = Enumerable
                .Repeat(0, 1000)
                .Select(i => random.Next(10000, int.MaxValue))
                .ToArray();

            var exception = Record.Exception(() => Parallel.ForEach(ids4Test, id =>
                tgBotServiceMock.Raise(x => x.OnMessage += null, null,
                    Mock.Of<ITgInboundMessage>(m => m.Message == "Привет" && m.ChatId == id))));

            Assert.Null(exception);

            playBillResolverMock.Verify(x => x.RequestProcess(It.IsAny<IPerformanceFilter>()
                    , It.IsAny<CancellationToken>()), Times.Never);
        }
    }

}
