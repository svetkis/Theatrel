using theatrel.Interfaces;

namespace theatrel.TLBot.Interfaces
{
    public interface ITLBotProcessor : IDIRegistrableService
    {
        void Start(ITLBotService botService);
    }
}
