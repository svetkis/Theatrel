using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.EncodingService;

public interface IEncodingService : IDISingleton
{
    public byte[] ProcessBytes(byte[] bytesData);
}