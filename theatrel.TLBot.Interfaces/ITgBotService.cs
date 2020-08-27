using System;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;

namespace theatrel.TLBot.Interfaces
{
    public interface ITgBotService : IDIRegistrable
    {
        event EventHandler<ITgInboundMessage> OnMessage;

        Task<bool> SendMessageAsync(long chatId, ITgOutboundMessage tlMessage);
        Task<bool> SendMessageAsync(long chatId, string message);
        void Start(CancellationToken cancellationToken);
        void Stop();
    }
}
