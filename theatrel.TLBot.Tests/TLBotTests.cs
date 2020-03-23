using Autofac;
using Moq;
using NSubstitute;
using System;
using System.Linq;
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
        [InlineData(4, new[]{DayOfWeek.Saturday}, "концерт", "пр»вет", "апрель", "—уббота", "кќнцерт")]
        [InlineData(6, new[]{ DayOfWeek.Sunday }, "опера"  , "Hi",     "июнь",   "вс", "опера")]
        [InlineData(7, new[] { DayOfWeek.Friday }, "балет", "ƒобрый деЌь!", "июль", "5", "Ѕалет")]
        public void DialogTest(int month, DayOfWeek[] dayOfWeeks, string perfomanceType, params string[] commands)
        {
            var playBillResolverMock = new PlayBillResolverMock();

            using (var scope = DIContainerHolder.RootScope.BeginLifetimeScope( builder =>
                  {
                      builder.RegisterInstance(playBillResolverMock.Object).As<IPlayBillDataResolver>().AsImplementedInterfaces();
                      builder.RegisterType<FilterHelper>().As<IFilterHelper>().AsImplementedInterfaces();
                      builder.RegisterType<TLBotProcessor>().As<ITLBotProcessor>().AsImplementedInterfaces();
                  }))
            {
                var test = scope.Resolve<IPlayBillDataResolver>();

                var tlProcessor = scope.Resolve<ITLBotProcessor>();

                var tlBotServiceMock = new Mock<ITLBotService>(MockBehavior.Strict);
                tlBotServiceMock.Setup(x => x.Start()).Verifiable();
                tlBotServiceMock.Setup(x => x.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>())).Verifiable();

                tlProcessor.Start(tlBotServiceMock.Object);

                foreach (var cmd in commands)
                    tlBotServiceMock.Raise(x => x.OnMessage += null, null, GetMessageEventArgs(cmd));

                Task.Delay(300).GetAwaiter().GetResult();

                Assert.True(playBillResolverMock.Filter != null);
                Assert.True(playBillResolverMock.Filter.DaysOfWeek.OrderBy(d => d).SequenceEqual(dayOfWeeks.OrderBy(d => d)));
                Assert.True(playBillResolverMock.StartDate.Month == month);
                Assert.True( perfomanceType.Equals(playBillResolverMock.Filter.PerfomanceTypes.First(), StringComparison.InvariantCultureIgnoreCase));

                tlBotServiceMock.Verify();

                //second dialog after first
                foreach (var cmd in commands)
                    tlBotServiceMock.Raise(x => x.OnMessage += null, null, GetMessageEventArgs(cmd));

                Task.Delay(300).GetAwaiter().GetResult();

                Assert.True(playBillResolverMock.Filter != null);
                Assert.True(playBillResolverMock.Filter.DaysOfWeek.OrderBy(d => d).SequenceEqual(dayOfWeeks.OrderBy(d => d)));
                Assert.True(playBillResolverMock.StartDate.Month == month);
                Assert.True(perfomanceType.Equals(playBillResolverMock.Filter.PerfomanceTypes.First(), StringComparison.InvariantCultureIgnoreCase));
            }
        }
    }
}
