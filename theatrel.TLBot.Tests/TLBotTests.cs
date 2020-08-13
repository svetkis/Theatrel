using Autofac;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces;
using theatrel.Lib;
using theatrel.TLBot.Interfaces;
using Xunit;

namespace theatrel.TLBot.Tests
{
    public class TLBotTests
    {
        [Theory]
        [InlineData(5, new[] { DayOfWeek.Monday }, "концерт", "пр»вет", "апрель", "Ќет!", "май", "—уббота", "нет", "понедельник", "кќнцерт")]
        [InlineData(5, new[] { DayOfWeek.Monday }, "концерт", "пр»вет", "апрель", "¬ыбрать другой мес€ц", "май", "—уббота", "нет", "понедельник", "кќнцерт")]
        [InlineData(4, new[] { DayOfWeek.Saturday}, "концерт", "пр»вет", "апрель", "—уббота", "кќнцерт")]
        [InlineData(6, new[] { DayOfWeek.Sunday }, "ќпера"  , "Hi", "июнь",   "вс", "опера")]
        [InlineData(7, new[] { DayOfWeek.Friday }, "балет", "ƒобрый деЌь!", "июль", "5", "Ѕалет")]
        [InlineData(5, new[] { DayOfWeek.Friday }, "балет", "ƒобрый деЌь!", "июль", "привет!", "май", "5", "Ѕалет")]
        public async Task DialogTest(int month, DayOfWeek[] dayOfWeeks, string performanceType, params string[] commands)
        {
            IPerformanceFilter filter = null;

            var playBillResolverMock = new Mock<IPlayBillDataResolver>();
            playBillResolverMock.Setup(h => h.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()))
                .Callback<IPerformanceFilter, CancellationToken>((filterResult, cToken) =>
                {
                    filter = filterResult;
                }).Returns(() => Task.FromResult(new IPerformanceData[0]));

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

            //test
            tlProcessor.Start(tlBotServiceMock.Object, CancellationToken.None);

            foreach (var cmd in commands)
                tlBotServiceMock.Raise(x => x.OnMessage += null, null,
                    Mock.Of<ITLMessage>(m => m.Message == cmd && m.ChatId == 1));

            playBillResolverMock.Verify(lw => lw.RequestProcess(It.IsAny<IPerformanceFilter>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce());

            Assert.NotNull(filter);
            Assert.True(filter.DaysOfWeek.OrderBy(d => d).SequenceEqual(dayOfWeeks.OrderBy(d => d)));
            Assert.Equal(month, filter.StartDate.Month);
            Assert.Equal(performanceType.ToLower(), filter.PerformanceTypes.First().ToLower());

            tlBotServiceMock.Verify();

            //second dialog after first
            foreach (var cmd in commands)
                tlBotServiceMock.Raise(x => x.OnMessage += null, null,
                    Mock.Of<ITLMessage>(m => m.Message == cmd && m.ChatId == 1));

            Assert.NotNull(filter);
            Assert.True(filter.DaysOfWeek.OrderBy(d => d).SequenceEqual(dayOfWeeks.OrderBy(d => d)));
            Assert.Equal(month, filter.StartDate.Month);
            Assert.Equal(performanceType.ToLower(), filter.PerformanceTypes.First().ToLower());
        }
    }
}
