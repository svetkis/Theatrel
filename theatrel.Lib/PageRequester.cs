using Polly;
using RestSharp;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
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
                    .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
                    .ExecuteAsync(async () =>
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            IRestResponse response = await client.ExecuteAsync(request, cancellationToken);

                            if (response.Content.Contains("Страница не найдена"))
                                throw new HttpRequestException();

                            if (response.StatusCode == HttpStatusCode.ServiceUnavailable || response.StatusCode == HttpStatusCode.NotFound)
                                throw new HttpRequestException();

                            if (response.StatusCode != HttpStatusCode.OK)
                                return null;

                            if (!response.ContentType.Contains("1251"))
                                return response.Content;

                            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                            Encoding encoding = Encoding.GetEncoding("windows-1251");
                            string result = Encoding.UTF8.GetString(Encoding.Convert(encoding, Encoding.UTF8, response.RawBytes))
                                .Replace("windows-1251", "utf-8");

                            return result;
                        });
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
