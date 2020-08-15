using System;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces;

namespace theatrel.TLBot.Interfaces
{
    public interface ITLBotService : IDISingleton
    {
        event EventHandler<ITlInboundMessage> OnMessage;

        Task<bool> SendMessageAsync(long chatId, ITlOutboundMessage tlMessage);
        Task<bool> SendMessageAsync(long chatId, string message);
        void Start(CancellationToken cancellationToken);
        void Stop();
    }
}
