using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.Helpers
{
    public interface IPageRequester : IDIRegistrable
    {
        Task<string> Request(string url, CancellationToken cancellationToken);
    }
}
