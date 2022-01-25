using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.Helpers
{
    public interface IPageRequester : IDIRegistrable
    {
        Task<byte[]> RequestBytes(string url, bool needEncoding, CancellationToken cancellationToken);
    }
}
