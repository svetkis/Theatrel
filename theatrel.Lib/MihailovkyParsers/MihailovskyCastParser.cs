using System;
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

    public async Task<IPerformanceCast> ParseFromUrl(string url, string castFromPlaybill, bool wasMoved, CancellationToken cancellationToken)
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

        return await PrivateParse(content, castFromPlaybill, cancellationToken);
    }

    private async Task<IPerformanceCast> PrivateParse(byte[] data, string playbillCast, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var castBlocks = await GetCastBlocks(data, cancellationToken);

            PerformanceCast performanceCast = new();

            foreach (var block in castBlocks)
            {
                await ParseBlock(block, performanceCast, false, cancellationToken);
            }

            if (playbillCast != null)
            {
                var additionalBlocks = await GetCastBlocks(playbillCast, cancellationToken);
                foreach (var block in additionalBlocks)
                {
                    await ParseBlock(block, performanceCast, true, cancellationToken);
                }
            }

            performanceCast.State = performanceCast.Cast.Any() ? CastState.Ok : CastState.CastIsNotSet;

            return performanceCast;
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"Performance cast exception {ex.Message} {ex.StackTrace}");
        }

        return new PerformanceCast { State = CastState.TechnicalError };
    }

    private async Task ParseBlock(
        IElement castBlock,
        PerformanceCast performanceCast,
        bool additionalInfo,
        CancellationToken cancellationToken)
    {
        IEnumerable<string> lines = castBlock.InnerHtml
            .Split(new[] { "<br/>", "<br>", "</p>", "<p>" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            string characterName = line.GetCharacterName();

            if (CommonTags.TechnicalTagsInCastList.Any(tag => characterName.StartsWith(tag, StringComparison.InvariantCultureIgnoreCase)))
                continue;

            var actors = await GetCastInfo(line, cancellationToken);

            MergeCast(performanceCast, characterName, actors, additionalInfo);
        }
    }

    private void MergeCast(PerformanceCast cast, string characterName, IList<IActor> actors, bool additionalInfo)
    {
        if (null == actors || !actors.Any())
            return;

        var newActors = new List<IActor>();

        foreach (var actor in actors)
        {
            if (!additionalInfo || !cast.Cast.Any(x => x.Value.Any(y => IsActorsEqual(y, actor))))
                newActors.Add(actor);
        }

        if (!newActors.Any())
            return;

        if (!cast.Cast.ContainsKey(characterName))
        {
            cast.Cast[characterName] = newActors;
        }
        else
        {
            (cast.Cast[characterName] as List<IActor>).AddRange(newActors);
        }
    }

    private bool IsActorsEqual(IActor actor1, IActor actor2)
    {
        if (string.Equals(actor1.Url, actor2.Url, StringComparison.InvariantCultureIgnoreCase))
            return true;

        return string.Equals(actor1.Name, actor2.Name, StringComparison.InvariantCultureIgnoreCase);
    }

    private async Task<IElement[]> GetCastBlocks(byte[] data, CancellationToken cancellationToken)
    {
        using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        await using var stream = new MemoryStream(data);
        using IDocument parsedDoc = await context.OpenAsync(req => req.Content(stream), cancellationToken);

        return GetElements(parsedDoc);
    }

    private async Task<IElement[]> GetCastBlocks(string castFromPlaybill, CancellationToken cancellationToken)
    {
        using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        using IDocument document = await context.OpenAsync(req => req.Content(castFromPlaybill), cancellationToken);

        return GetElements(document);
    }

    private IElement[] GetElements(IDocument document)
    {
        var dlBlock = document.QuerySelectorAll("dl").FirstOrDefault(x =>
        {
            var dtBlocks = x.QuerySelectorAll("dt");

            return dtBlocks.Any() &&
                string.Equals(dtBlocks.First().TextContent, "исполнители", StringComparison.InvariantCultureIgnoreCase);
        });

        if (dlBlock != null)
        {
            return new IElement[] { dlBlock.Children.Last() };
        }

        return document.QuerySelectorAll("p.f-ap").ToArray();
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