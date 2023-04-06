using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.VkIntegration;

public interface IVkIntegration : IDISingleton
{
    Task<bool> SendMessage(long vkId, string message);
}