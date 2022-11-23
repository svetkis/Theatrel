using System.Text;
using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.EncodingService;

public interface IEncodingService : IDISingleton
{
    Encoding Get1251Encoding();
    public byte[] ProcessBytes(byte[] bytesData);
}