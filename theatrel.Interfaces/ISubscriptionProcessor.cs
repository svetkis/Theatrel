using System.Threading.Tasks;

namespace theatrel.Interfaces
{
    public interface ISubscriptionProcessor : IDISingleton
    {
        Task<bool> ProcessSubscriptions();
    }
}
