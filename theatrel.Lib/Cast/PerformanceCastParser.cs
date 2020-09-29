using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using theatrel.Common;
using theatrel.Common.Enums;
using theatrel.Interfaces.Cast;

namespace theatrel.Lib.Cast
{
    internal class PerformanceCastParser : IPerformanceCastParser
    {
        public async Task<IPerformanceCast> ParseFromUrl(string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(url))
                return new PerformanceCast {State = CastState.CastIsNotSet };

            switch (url)
            {
                case CommonTags.NotDefinedTag:
                    return new PerformanceCast { State = CastState.CastIsNotSet };
                case CommonTags.WasMovedTag:
                    return new PerformanceCast { State = CastState.PerformanceWasMoved };
            }

            var content = await PageRequester.Request(url, cancellationToken);
            return await PrivateParse(content, cancellationToken);
        }

        public async Task<IPerformanceCast> Parse(string data, CancellationToken cancellationToken)
            => await PrivateParse(data, cancellationToken);

        private async Task<IPerformanceCast> PrivateParse(string data, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(data))
                return new PerformanceCast { State = CastState.TechnicalError };

            try
            {
                var context = BrowsingContext.New(Configuration.Default);
                var parsedDoc = await context.OpenAsync(req => req.Content(data), cancellationToken);

                var castBlock = parsedDoc.All.FirstOrDefault(m => m.ClassList.Contains("sostav") && m.ClassList.Contains("inf_block"));

                cancellationToken.ThrowIfCancellationRequested();

                if (castBlock == null)
                    return new PerformanceCast { State = CastState.CastIsNotSet };

                IElement paragraph = castBlock.Children.Last();
                if (!paragraph.Children.Any())
                    return new PerformanceCast {State = CastState.CastIsNotSet};

                string text = paragraph.Children[0].InnerHtml.Trim();
                var lines = text.Split(new[] { "<br/>", "<br>" }, StringSplitOptions.RemoveEmptyEntries);

                PerformanceCast performanceCast = new PerformanceCast { State = CastState.Ok, Cast = new Dictionary<string, IActor>() };

                foreach (var line in lines)
                {
                    string name = line.Split('–').First().Replace("&nbsp;", " ").Trim();

                    var parsedLine = await context.OpenAsync(req => req.Content(line), cancellationToken);

                    IElement[] allElementChildren = parsedLine.QuerySelectorAll("*").ToArray();

                    IElement aTag = allElementChildren.FirstOrDefault(m => m.LocalName == "a");

                    string actorName = aTag?.TextContent.Replace("&nbsp;", " ").Trim();
                    actorName = string.IsNullOrEmpty(actorName) ? CommonTags.NotDefinedTag : actorName;

                    performanceCast.Cast[name] = new PerformanceActor { Name = actorName, Url = ProcessUrl(aTag) };
                }

                return performanceCast;

            }
            catch (Exception ex)
            {
                Trace.TraceInformation($"Performance cast exception {ex.Message} {ex.StackTrace}");
            }

            return new PerformanceCast { State = CastState.TechnicalError };
        }

        private string ProcessUrl(IElement urlData)
        {
            string url = urlData?.GetAttribute("href").Trim();
            if (string.IsNullOrEmpty(url) || url == CommonTags.JavascriptVoid)
                return CommonTags.NotDefinedTag;

            return url.StartsWith("/") ? $"https://www.mariinsky.ru{url}" : url;
        }

    }
}
