using System;
using theatrel.Interfaces;

namespace theatrel.TLBot.Interfaces
{
    public interface ITLBotService : IDIRegistrableService
    {
        event EventHandler<ITLMessage> OnMessage;

        void SendMessageAsync(long chatId, string message);
        void Start();
        void Stop();
    }
}
