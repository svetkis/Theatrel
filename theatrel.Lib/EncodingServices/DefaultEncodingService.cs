using theatrel.Interfaces.EncodingService;

namespace theatrel.Lib.EncodingServices;

internal class DefaultEncodingService : IEncodingService
{
    public string Process(string data) => data;
}