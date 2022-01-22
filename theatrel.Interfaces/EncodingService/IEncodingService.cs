using theatrel.Interfaces.Autofac;

namespace theatrel.Interfaces.EncodingService;

public interface IEncodingService : IDIRegistrable
{
    public string Process(string data, byte[] bytesData);
}