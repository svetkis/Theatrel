using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.Subscriptions
{
    public interface ISubscriptionProcessor : IDISingleton
    {
        Task<bool> ProcessSubscriptions();
    }
}
