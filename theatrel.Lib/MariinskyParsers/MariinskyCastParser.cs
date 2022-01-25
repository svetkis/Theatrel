using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using theatrel.Common;
using theatrel.Common.Enums;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.Helpers;
using theatrel.Lib.Cast;

namespace theatrel.Lib.MariinskyParsers;

internal class MariinskyCastParser : IPerformanceCastParser
{
    private readonly IPageRequester _pageRequester;

    public MariinskyCastParser(IPageRequester pageRequester)
    {
        _pageRequester = pageRequester;
    }

    public async Task<IPerformanceCast> ParseFromUrl(string url, bool wasMoved, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(url) || wasMoved)
            return new PerformanceCast {State = CastState.CastIsNotSet, Cast = new Dictionary<string, IList<IActor>>()};

        switch (url)
        {
            case CommonTags.NotDefinedTag:
                return new PerformanceCast { State = CastState.CastIsNotSet };
            case CommonTags.WasMovedTag:
                return new PerformanceCast { State = CastState.PerformanceWasMoved };
        }

        var content = await _pageRequester.RequestBytes(url, false, cancellationToken);
        if (null == content)
            return new PerformanceCast {State = CastState.TechnicalError};

        return await PrivateParse(content, cancellationToken);
    }

    public async Task<IPerformanceCast> Parse(byte[] data, CancellationToken cancellationToken)
        => await PrivateParse(data, cancellationToken);

    private async Task<IPerformanceCast> PrivateParse(byte[] data, CancellationToken cancellationToken)
    {
        if (data == null || !data.Any())
            return new PerformanceCast { State = CastState.TechnicalError };

        try
        {
            using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
            await using MemoryStream dataStream = new MemoryStream(data);
            using IDocument parsedDoc = await context.OpenAsync(req => req.Content(dataStream), cancellationToken);

            IElement castBlock = parsedDoc.All.FirstOrDefault(m => m.ClassList.Contains("sostav") && m.ClassList.Contains("inf_block"));

            cancellationToken.ThrowIfCancellationRequested();

            if (castBlock == null)
                return new PerformanceCast { State = CastState.CastIsNotSet, Cast = new Dictionary<string, IList<IActor>>()};

            PerformanceCast performanceCast = new PerformanceCast { State = CastState.Ok, Cast = new Dictionary<string, IList<IActor>>() };

            IElement conductor = castBlock.Children.FirstOrDefault(e => e.ClassName == "conductor");
            if (conductor != null)
            {
                var actors = GetCastInfo(conductor.QuerySelectorAll("*").Where(m => m.LocalName == "a").ToArray());
                if (null != actors && actors.Any())
                    performanceCast.Cast[CommonTags.Conductor] = actors;
            }

            IElement paragraph = castBlock.Children.Last();
            if (!paragraph.Children.Any())
                return new PerformanceCast { State = CastState.CastIsNotSet, Cast = new Dictionary<string, IList<IActor>>() };

            string text = paragraph.InnerHtml.Trim();
            var lines = text.Split(new[] { "<br/>", "<br>", "</p>", "<p>" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.StartsWith(CommonTags.Phonogram, StringComparison.OrdinalIgnoreCase))
                {
                    performanceCast.Cast[CommonTags.Conductor] = new List<IActor>
                        {new PerformanceActor{ Name = CommonTags.Phonogram, Url = CommonTags.NotDefinedTag}};

                    continue;
                }

                string characterName = line.Contains('–') || line.Contains(':')
                    ? line.Split('–', ':').First().Replace("&nbsp;", " ").Trim()
                    : CommonTags.Actor;

                using IDocument parsedLine = await context.OpenAsync(req => req.Content(line), cancellationToken);
                IElement[] allElementChildren = parsedLine.QuerySelectorAll("*").ToArray();

                if (CommonTags.TechnicalTagsInCastList.Any(tag => characterName.StartsWith(tag)))
                    continue;

                IList<IActor> actors = GetCastInfo(allElementChildren);
                if (null == actors || !actors.Any())
                    continue;

                if (performanceCast.Cast.ContainsKey(characterName))
                {
                    foreach (var actor in actors)
                        performanceCast.Cast[characterName].Add(actor);
                }
                else
                {
                    performanceCast.Cast[characterName] = actors;
                }
            }

            return performanceCast;

        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"Performance cast exception {ex.Message} {ex.StackTrace}");
        }

        return new PerformanceCast { State = CastState.TechnicalError };
    }

    private static IList<IActor> GetCastInfo(IElement[] allElementChildren)
    {
        IElement[] aTags = allElementChildren.Where(m => m.LocalName == "a").ToArray();

        var actors = new List<IActor>();

        foreach (var aTag in aTags)
        {
            string actorName = aTag?.TextContent.Replace("&nbsp;", " ").Trim();
            if (!string.IsNullOrEmpty(actorName))
                actors.Add(new PerformanceActor { Name = actorName, Url = ProcessUrl(aTag) });
        }

        return actors;
    }

    private static string ProcessUrl(IElement urlData)
    {
        string url = urlData?.GetAttribute("href").Trim();
        if (string.IsNullOrEmpty(url) || url == CommonTags.JavascriptVoid)
            return CommonTags.NotDefinedTag;

        return url.StartsWith("/") ? $"https://www.mariinsky.ru{url}" : url;
    }
}