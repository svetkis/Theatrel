using Polly;
using RestSharp;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using theatrel.Interfaces.EncodingService;
using theatrel.Interfaces.Helpers;

namespace theatrel.Lib;

internal class PageRequester : IPageRequester
{
    private readonly IEncodingService _encodingService;

    public PageRequester(IEncodingService encodingService)
    {
        _encodingService = encodingService;
    }

    public async Task<byte[]> RequestBytes(string url, CancellationToken cancellationToken)
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

                    if (response.Content == null || response.Content.Contains("Страница не найдена"))
                        throw new HttpRequestException();

                    if (response.StatusCode == HttpStatusCode.ServiceUnavailable || response.StatusCode == HttpStatusCode.NotFound
                        || response.ContentLength == 0)
                        throw new HttpRequestException();

                    if (response.StatusCode != HttpStatusCode.OK)
                        return null;

                    return _encodingService.ProcessBytes(response.RawBytes);
                });
        }
        catch (Exception)
        {
            return null;
        }
    }
}