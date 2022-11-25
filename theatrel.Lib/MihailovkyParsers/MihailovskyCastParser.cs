﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using theatrel.Common;
using theatrel.Common.Enums;
using theatrel.Interfaces.Cast;
using theatrel.Interfaces.Helpers;
using theatrel.Lib.Cast;
using theatrel.Lib.Utils;

namespace theatrel.Lib.MihailovkyParsers;

internal class MihailovskyCastParser : IPerformanceCastParser
{
    private readonly IPageRequester _pageRequester;

    public MihailovskyCastParser(IPageRequester pageRequester)
    {
        _pageRequester = pageRequester;
    }

    public async Task<IPerformanceCast> ParseFromUrl(string url, bool wasMoved, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(url) || wasMoved)
            return new PerformanceCast { State = CastState.CastIsNotSet, Cast = new Dictionary<string, IList<IActor>>() };

        switch (url)
        {
            case CommonTags.NotDefinedTag:
                return new PerformanceCast { State = CastState.CastIsNotSet };
            case CommonTags.WasMovedTag:
                return new PerformanceCast { State = CastState.PerformanceWasMoved };
        }

        var content = await _pageRequester.RequestBytes(url, true, cancellationToken);
        if (null == content)
            return new PerformanceCast { State = CastState.TechnicalError };

        return await PrivateParse(content, cancellationToken);
    }

    public async Task<IPerformanceCast> ParseText(string data, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(data))
            return new PerformanceCast { State = CastState.CastIsNotSet };

        return await PrivateParse(Encoding.UTF8.GetBytes(data), cancellationToken);
    }

    private async Task<IPerformanceCast> PrivateParse(byte[] data, CancellationToken cancellationToken)
    {
        if (data == null || !data.Any())
            return new PerformanceCast { State = CastState.TechnicalError };

        try
        {
            using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
            await using var stream = new MemoryStream(data);
            using IDocument parsedDoc = await context.OpenAsync(req => req.Content(stream), cancellationToken);

            IElement[] castBlock = parsedDoc.QuerySelectorAll("p.f-ap").ToArray();

            cancellationToken.ThrowIfCancellationRequested();

            if (!castBlock.Any())
                return new PerformanceCast { State = CastState.CastIsNotSet, Cast = new Dictionary<string, IList<IActor>>() };

            PerformanceCast performanceCast = new PerformanceCast { State = CastState.Ok, Cast = new Dictionary<string, IList<IActor>>() };

            IEnumerable<string> lines = castBlock.SelectMany(c =>
                c.InnerHtml.Split(new[] {"<br/>", "<br>", "</p>", "<p>"}, StringSplitOptions.RemoveEmptyEntries));

            foreach (var line in lines)
            {
                string characterName = line.GetCharacterName();

                if (CommonTags.TechnicalTagsInCastList.Any(tag => characterName.StartsWith(tag)))
                    continue;

                var actors = await GetCastInfo(line, cancellationToken);
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

    private static async Task<IList<IActor>> GetCastInfo(string line, CancellationToken cancellationToken)
    {
        using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        using IDocument parsedLine = await context.OpenAsync(req => req.Content(line), cancellationToken);
        var aTags = parsedLine.QuerySelectorAll("a");

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

        return url.StartsWith("/") ? $"https://mikhailovsky.ru{url}" : url;
    }
}