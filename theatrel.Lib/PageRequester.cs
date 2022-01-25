using Polly;
using RestSharp;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.EncodingService;
using theatrel.Interfaces.Helpers;

namespace theatrel.Lib;

internal class PageRequester : IPageRequester
{
    private readonly IEncodingService _encodingService;
    private readonly byte[] _notFoundLabelBytes = Encoding.UTF8.GetBytes("Страница не найдена");

    public PageRequester(IEncodingService encodingService)
    {
        _encodingService = encodingService;
    }

    public async Task<byte[]> RequestBytes(string url, bool needEncoding, CancellationToken cancellationToken)
    {
        try
        {
            using RestClient client = new RestClient(url);
            RestRequest request = new RestRequest { Method = Method.Get };

            return await Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                .ExecuteAsync(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    RestResponse response = await client.ExecuteAsync(request, cancellationToken);

                    int contentCharsetIndex = response.RawBytes.AsSpan().IndexOf(_notFoundLabelBytes);
                    if (-1 != contentCharsetIndex)
                        throw new HttpRequestException();

                    if (response.RawBytes == null || response.StatusCode == HttpStatusCode.ServiceUnavailable
                                                  || response.StatusCode == HttpStatusCode.NotFound
                                                  || response.RawBytes?.Length == 0)
                        throw new HttpRequestException();

                    if (response.StatusCode != HttpStatusCode.OK)
                        return null;

                    return needEncoding ? _encodingService.ProcessBytes(response.RawBytes) : response.RawBytes;
                });
        }
        catch (Exception)
        {
            return null;
        }
    }
}