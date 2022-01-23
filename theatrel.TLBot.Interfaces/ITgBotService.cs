using System;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;

namespace theatrel.TLBot.Interfaces;

public interface ITgBotService : IDISingleton
{
    event EventHandler<ITgInboundMessage> OnMessage;

    Task<bool> SendMessageAsync(long chatId, ITgOutboundMessage message, CancellationToken cancellationToken);
    Task<bool> SendMessageAsync(long chatId, string message, CancellationToken cancellationToken);
    Task<bool> SendEscapedMessageAsync(long chatId, string message, CancellationToken cancellationToken);

    void Start();
    void Stop();
}