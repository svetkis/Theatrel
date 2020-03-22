using theatrel.Interfaces;

namespace theatrel.TLBot.Interfaces
{
    public interface ITLBotProcessor : IDISingleton
    {
        void Start(ITLBotService botService);
    }
}
