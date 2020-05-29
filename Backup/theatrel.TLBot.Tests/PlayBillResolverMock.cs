using Moq;
using System;
using System.Threading.Tasks;
using theatrel.Interfaces;

namespace theatrel.TLBot.Tests
{
    internal class PlayBillResolverMock
    {
        public DateTime StartDate = new DateTime();
        public IPerformanceFilter Filter = null;

        private Mock<IPlayBillDataResolver> _playBillResolverMock = new Mock<IPlayBillDataResolver>();
        public IPlayBillDataResolver Object => _playBillResolverMock.Object;

        public PlayBillResolverMock()
        {
            _playBillResolverMock.Setup(h => h.RequestProcess(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<IPerformanceFilter>()))
                    .Callback<DateTime, DateTime, IPerformanceFilter>((dtStart, stEnd, filterResult) =>
                    {
                        StartDate = dtStart;
                        Filter = filterResult;
                    }).Returns(() => Task.FromResult(new IPerformanceData[0]));
        }
    }
}
