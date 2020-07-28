using Autofac;
using Moq;
using NSubstitute;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces;
using theatrel.TLBot.Interfaces;
using Xunit;

namespace theatrel.TLBot.Tests
{
    public class TLBotTests
    {
        private ITLMessage GetMessageEventArgs(string message)
        {
            var msgMock = Substitute.For<ITLMessage>();
            msgMock.ChatId.Returns(1);
            msgMock.Message.Returns(message);

            return msgMock;
        }

        [Theory]
        [InlineData(5, new[] { DayOfWeek.Monday }, "концерт", "пр»вет", "апрель", "Ќет!", "май", "—уббота", "нет", "понедельник", "кќнцерт")]
        [InlineData(4, new[] { DayOfWeek.Saturday}, "концерт", "пр»вет", "апрель", "—уббота", "кќнцерт")]
        [InlineData(6, new[] { DayOfWeek.Sunday }, "ќпера"  , "Hi", "июнь",   "вс", "опера")]
        [InlineData(7, new[] { DayOfWeek.Friday }, "балет", "ƒобрый деЌь!", "июль", "5", "Ѕалет")]
        [InlineData(5, new[] { DayOfWeek.Friday }, "балет", "ƒобрый деЌь!", "июль", "привет!", "май", "5", "Ѕалет")]
        public async Task DialogTest(int month, DayOfWeek[] dayOfWeeks, string performanceType, params string[] commands)
        {
            var playBillResolverMock = new PlayBillResolverMock();

            await using ILifetimeScope scope = DIContainerHolder.RootScope.BeginLifetimeScope( builder =>
            {
                builder.RegisterInstance(playBillResolverMock.Object).As<IPlayBillDataResolver>().AsImplementedInterfaces();
                builder.RegisterType<FilterHelper>().As<IFilterHelper>().AsImplementedInterfaces();
                builder.RegisterType<TLBotProcessor>().As<ITLBotProcessor>().AsImplementedInterfaces();
            });

            var tlProcessor = scope.Resolve<ITLBotProcessor>();

            var tlBotServiceMock = new Mock<ITLBotService>(MockBehavior.Strict);
            tlBotServiceMock.Setup(x => x.Start(CancellationToken.None)).Verifiable();
            tlBotServiceMock.Setup(x => x.SendMessageAsync(It.IsAny<long>(), It.IsAny<ICommandResponse>())).Verifiable();

            tlProcessor.Start(tlBotServiceMock.Object, CancellationToken.None);

            foreach (var cmd in commands)
                tlBotServiceMock.Raise(x => x.OnMessage += null, null, GetMessageEventArgs(cmd));

            Assert.NotNull(playBillResolverMock.Filter);
            Assert.True(playBillResolverMock.Filter.DaysOfWeek.OrderBy(d => d).SequenceEqual(dayOfWeeks.OrderBy(d => d)));
            Assert.Equal(month, playBillResolverMock.StartDate.Month);
            Assert.Equal(performanceType.ToLower(), playBillResolverMock.Filter.PerformanceTypes.First().ToLower());

            tlBotServiceMock.Verify();

            //second dialog after first
            foreach (var cmd in commands)
                tlBotServiceMock.Raise(x => x.OnMessage += null, null, GetMessageEventArgs(cmd));

            Assert.NotNull(playBillResolverMock.Filter);
            Assert.True(playBillResolverMock.Filter.DaysOfWeek.OrderBy(d => d).SequenceEqual(dayOfWeeks.OrderBy(d => d)));
            Assert.Equal(month, playBillResolverMock.StartDate.Month);
            Assert.Equal(performanceType.ToLower(), playBillResolverMock.Filter.PerformanceTypes.First().ToLower());
        }
    }
}
