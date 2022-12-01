using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

    public async Task<IPerformanceCast> ParseText(string data, string additionalData, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(data))
            return new PerformanceCast { State = CastState.CastIsNotSet };

        return await PrivateParse(Encoding.UTF8.GetBytes(data), additionalData, cancellationToken);
    }

    private string[] technicalActorStrings = { "будет обьявлено позднее", "будет объявлено позднее", "музыкальный спектакль" };

    private async Task<IPerformanceCast> PrivateParse(byte[] data, string castFromPlaybill, CancellationToken cancellationToken)
    {
        if (data == null || !data.Any())
            return new PerformanceCast()
            {
                State = CastState.CastIsNotSet
            };

        try
        {
            PerformanceCast performanceCast = new PerformanceCast
            {
                Cast = new Dictionary<string, IList<IActor>>()
            };

            using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
            await using MemoryStream dataStream = new MemoryStream(data);
            using IDocument parsedDoc = await context.OpenAsync(req => req.Content(dataStream), cancellationToken);

            IElement castBlock = parsedDoc.GetBody().QuerySelector("div.sostav.inf_block");

            cancellationToken.ThrowIfCancellationRequested();

            if (castBlock == null)
                return performanceCast;

            IElement conductor = castBlock.QuerySelector(".conductor");
            if (conductor != null)
            {
                var actors = GetCastInfo(conductor.QuerySelectorAll("a").ToArray());
                if (null != actors && actors.Any())
                    performanceCast.Cast[CommonTags.Conductor] = actors;
            }

            IElement paragraph = castBlock.Children.Last();
            if (!paragraph.Children.Any())
                return new PerformanceCast { State = CastState.CastIsNotSet, Cast = new Dictionary<string, IList<IActor>>() };

            await ParseText(paragraph.InnerHtml.Trim(), performanceCast, false, cancellationToken);

            await ParseText(castFromPlaybill, performanceCast, true, cancellationToken);

            performanceCast.State = performanceCast.Cast.Any() ? CastState.Ok : CastState.CastIsNotSet;

            return performanceCast;

        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"Performance cast exception {ex.Message} {ex.StackTrace}");
        }

        return new PerformanceCast { State = CastState.TechnicalError };
    }

    private async Task ParseText(
        string text,
        PerformanceCast performanceCast,
        bool additionalInfo,
        CancellationToken cancellationToken)
    {
        //detete commented
        text = Regex.Replace(text, "<!--.*?-->", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = text.Replace("При участии", "");
        
        var lines = text
            .Trim()
            .Split(new[] { "<br/>", "<br>", "</p>", "<p>", "<br />" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            if (line.StartsWith(CommonTags.Phonogram, StringComparison.OrdinalIgnoreCase))
            {
                performanceCast.Cast[CommonTags.Conductor] = new List<IActor> { new PerformanceActor { Name = CommonTags.Phonogram, Url = CommonTags.NotDefinedTag } };
                continue;
            }

            string characterName = line.GetCharacterName();

            if (characterName.StartsWith("В главных", StringComparison.OrdinalIgnoreCase))
                characterName = CommonTags.Actor;

            if (characterName.Length > 100 ||
                CommonTags.TechnicalTagsInCastList.Any(tag => characterName.StartsWith(tag, StringComparison.InvariantCultureIgnoreCase)))
            {
                continue;
            }

            using IBrowsingContext context = BrowsingContext.New(Configuration.Default);
            using IDocument parsedLine = await context.OpenAsync(req => req.Content(line), cancellationToken);
            var aTags = parsedLine.QuerySelectorAll("a").ToArray();

            IList<IActor> actors;
            if (aTags.Any())
            {
                actors = GetCastInfo(aTags);

                if (actors.Any() && actors.First().Name.Length > 100)
                    continue;
            }
            else
            {
                var name = line.GetActorName();

                if (string.IsNullOrEmpty(name) ||
                    name.Length > 100 ||
                    technicalActorStrings.Any(x => name.Contains(x, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                actors = new List<IActor>() { new PerformanceActor { Name = name, Url = CommonTags.NotDefinedTag } };
            }

            if (null == actors || !actors.Any())
                continue;

            var newActors = new List<IActor>();

            foreach (var actor in actors)
            {
                if (!additionalInfo || !performanceCast.Cast.Any(x => x.Value.Any(y => string.Equals(y.Name, actor.Name, StringComparison.InvariantCultureIgnoreCase))))
                    newActors.Add(actor);
            }

            if (!newActors.Any())
                continue;

            if (!performanceCast.Cast.ContainsKey(characterName))
            {
                performanceCast.Cast[characterName] = newActors;
            }
            else
            {
                (performanceCast.Cast[characterName] as List<IActor>).AddRange(newActors);
            }
        }
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