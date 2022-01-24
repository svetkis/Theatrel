using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.EncodingService;

public interface IEncodingService : IDIRegistrable
{
    public byte[] ProcessBytes(byte[] bytesData);
}