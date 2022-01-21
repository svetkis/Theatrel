using System.Threading;
using theatrel.Interfaces.Autofac;

namespace theatrel.TLBot.Interfaces;

public interface ITgBotProcessor : IDIRegistrable
{
    void Start(ITgBotService botService, CancellationToken cancellationToken);
    void Stop();
}