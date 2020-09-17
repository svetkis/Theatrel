using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.Subscriptions
{
    public interface ISubscriptionsCleanupService : IDIRegistrable
    {
        Task<bool> CleanUp();
    }
}
