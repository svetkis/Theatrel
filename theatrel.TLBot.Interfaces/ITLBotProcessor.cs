using System.Threading;
using theatrel.Interfaces;

namespace theatrel.TLBot.Interfaces
{
    public interface ITLBotProcessor : IDISingleton
    {
        void Start(ITLBotService botService, CancellationToken cancellationToken);
        void Stop();
    }
}
