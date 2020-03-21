using Autofac;
using Moq;
using NSubstitute;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using theatrel.Interfaces;
using theatrel.Lib;
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
        public void DialogTest(int month, DayOfWeek[] dayOfWeeks, string perfomanceType, params string[] commands)
        {
            var playBillResolverMock = new Mock<IPlayBillDataResolver>();

            DateTime startDt = new DateTime();
            IPerformanceFilter filter = null;

            playBillResolverMock.Setup(h => h.RequestProcess(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IPerformanceFilter>()))
                .Callback<DateTime, DateTime, IPerformanceFilter>((dtStart, stEnd, filterResult) =>
                {
                    startDt = dtStart;
                    filter = filterResult;
                }).Returns(() => Task.FromResult(new IPerformanceData[0]));

            using (var scope = DIContainerHolder.RootScope.BeginLifetimeScope( builder =>
                  {
                      builder.RegisterInstance(playBillResolverMock.Object).As<IPlayBillDataResolver>().AsImplementedInterfaces();
                      builder.RegisterType<PerformanceFilter>().As<IPerformanceFilter>().AsImplementedInterfaces();
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
                {
                    Trace.TraceInformation($"{DateTime.Now:mm:ss.fff} {cmd}");
                    tlBotServiceMock.Raise(x => x.OnMessage += null, null, GetMessageEventArgs(cmd));
                }

                Task.Delay(300).GetAwaiter().GetResult();
                Trace.TraceInformation($"{DateTime.Now:mm:ss.fff} assert");

                Assert.True(filter != null);
                Assert.True(filter.DaysOfWeek.OrderBy(d => d).SequenceEqual(dayOfWeeks.OrderBy(d => d)));
                Assert.True(startDt.Month == month);

                Trace.TraceInformation($"{filter.PerfomanceTypes.First()} {perfomanceType} {perfomanceType.Equals(filter.PerfomanceTypes.First(), StringComparison.InvariantCultureIgnoreCase)}");
                Assert.True( perfomanceType.Equals(filter.PerfomanceTypes.First(), StringComparison.InvariantCultureIgnoreCase));

                tlBotServiceMock.Verify();
            }
        }
    }
}
