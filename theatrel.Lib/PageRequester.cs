using Polly;
using RestSharp;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace theatrel.Lib
{
    internal class PageRequester
    {
        private PageRequester()
        { }

        private static PageRequester _instance;
        public static PageRequester Instance => _instance ??= new PageRequester();

        public static async Task<string> Request(string url, CancellationToken cancellationToken)
        {
            try
            {
                RestClient client = new RestClient(url);
                RestRequest request = new RestRequest(Method.GET);

                return await Policy
                    .Handle<HttpRequestException>()
                    .WaitAndRetryAsync(4, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                    .ExecuteAsync(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    IRestResponse response = await client.ExecuteAsync(request, cancellationToken);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return response.Content;
                    }

                    Trace.TraceInformation($"{url} {response.StatusCode}");
                    Trace.TraceInformation($"status:{response.ResponseStatus} message:\"{response.ErrorMessage}\"");

                    if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                        throw new HttpRequestException();

                    return response.Content;
                });
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
