using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

namespace theatrel.Lib.MariinskyParsers;

internal class MariinskyCastParser : IPerformanceCastParser
{
    private readonly IPageRequester _pageRequester;

    public MariinskyCastParser(IPageRequester pageRequester)
    {
        _pageRequester = pageRequester;
    }

    public async Task<IPerformanceCast> ParseFromUrl(
        string url,
        string castFromPlaybill,
        bool wasMoved,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(url) || wasMoved)
        {
            return new PerformanceCast
            {
                State = CastState.CastIsNotSet
            };
        }

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

        return await PrivateParse(content, castFromPlaybill, cancellationToken);
    }

    private string[] technicalActorStrings = { "будет обьявлено позднее", "будет объявлено позднее", "музыкальный спектакль" };

    private async Task<IPerformanceCast> PrivateParse(byte[] data, string castFromPlaybill, CancellationToken cancellationToken)
    {
        try
        {
            IElement castBlock = await GetCastBlock(data, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            PerformanceCast performanceCast = new();
            if (castBlock != null)
            {
                foreach(var block in castBlock.Children)
                {
                    if (!ParseConductor(block, performanceCast))
                        await ParseText(block.InnerHtml.Trim(), performanceCast, false, cancellationToken);
                }
            }
            
            if (!string.IsNullOrEmpty(castFromPlaybill))
                await ParseText(castFromPlaybill, performanceCast, true, cancellationToken);

            performanceCast.State = performanceCast.Cast.Any() ? CastState.Ok : CastState.CastIsNotSet;

            return performanceCast;
        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"Cast PrivateParse exception {ex.Message} {ex.StackTrace}");
        }

        return new PerformanceCast { State = CastState.TechnicalError };
    }

    private async Task<IElement> GetCastBlock(byte[] data, CancellationToken cancellationToken)
    {
        using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        await using MemoryStream dataStream = new MemoryStream(data);
        using IDocument parsedDoc = await context.OpenAsync(req => req.Content(dataStream), cancellationToken);

        return parsedDoc.GetBody().QuerySelector("div.sostav.inf_block");
    }

    private bool ParseConductor(IElement block, PerformanceCast performanceCast)
    {
        if (block.ClassList.Contains("conductor"))
        {
            var actors = GetCastInfo(block.QuerySelectorAll("a").ToArray());
            if (null != actors && actors.Any())
            {
                performanceCast.Cast[CommonTags.Conductor] = actors;
                return true;
            }
        }

        return false;
    }

    private async Task ParseText(
        string text,
        PerformanceCast performanceCast,
        bool additionalInfo,
        CancellationToken cancellationToken)
    {
        //detete comments
        text = Regex.Replace(text, "<!--.*?-->", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = text.Replace("При участии", "");
        text = text.Replace(" />", "/>");
        text = text.Replace("target=\"_blank\"", "");
        text = text.Replace("target=\"blank\"", "");

        var lines = text
            .Trim()
            .Split(new[] { "<br/>", "<br>", "</p>", "<p>" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            if (line.StartsWith(CommonTags.Phonogram, StringComparison.OrdinalIgnoreCase))
            {
                performanceCast.Cast[CommonTags.Conductor] = new List<IActor> { new PerformanceActor { Name = CommonTags.Phonogram, Url = CommonTags.NotDefinedTag } };
                continue;
            }

            string characterName = line.GetCharacterName();

            if (characterName.Length > 100 || CommonTags.TechnicalTagsInCastList.Any(tag => characterName.StartsWith(tag, StringComparison.InvariantCultureIgnoreCase)))
            {
                continue;
            }

            if (characterName.StartsWith("В главных", StringComparison.OrdinalIgnoreCase))
                characterName = CommonTags.Actor;

            IList<IActor> actors = await ParseActorsFromLine(line, cancellationToken);

            MergeCast(performanceCast, characterName, actors, additionalInfo);
        }
    }

    private async Task<IList<IActor>> ParseActorsFromLine(string line, CancellationToken cancellationToken)
    {
        using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
        using IDocument parsedLine = await context.OpenAsync(req => req.Content(line), cancellationToken);
        IElement[] aTags = parsedLine.QuerySelectorAll("a").ToArray();

        if (aTags.Any())
        {
            IList<IActor> actors = GetCastInfo(aTags);

            if (actors.Any() && actors.First().Name.Length > 100)
                return null;
            else
                return actors;
        }
        else
        {
            var name = line.GetActorName();

            if (string.IsNullOrEmpty(name) ||
                name.Length > 100 ||
                technicalActorStrings.Any(x => name.Contains(x, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            return new List<IActor>() { new PerformanceActor { Name = name, Url = CommonTags.NotDefinedTag } };
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

    private static IList<IActor> GetCastInfo(IElement[] aTags)
    {
        var actors = new List<IActor>();

        foreach (var aTag in aTags)
        {
            string actorName = aTag.TextContent.Trim();
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