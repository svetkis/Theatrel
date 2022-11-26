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

    public async Task<IPerformanceCast> ParseText(string data, CancellationToken cancellationToken)
    {
        PerformanceCast performanceCast = new PerformanceCast { State = CastState.Ok, Cast = new Dictionary<string, IList<IActor>>() };
        await ParseText(data, performanceCast, cancellationToken);

        return performanceCast;
    }

    private string[] technicalActorStrings = { "будет обьявлено позднее", "будет объявлено позднее", "музыкальный спектакль" };

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
                var actors = GetCastInfo(conductor.QuerySelectorAll("a").ToArray());
                if (null != actors && actors.Any())
                    performanceCast.Cast[CommonTags.Conductor] = actors;
            }

            IElement paragraph = castBlock.Children.Last();
            if (!paragraph.Children.Any())
                return new PerformanceCast { State = CastState.CastIsNotSet, Cast = new Dictionary<string, IList<IActor>>() };

            await ParseText(paragraph.InnerHtml.Trim(), performanceCast, cancellationToken);

            return performanceCast;

        }
        catch (Exception ex)
        {
            Trace.TraceInformation($"Performance cast exception {ex.Message} {ex.StackTrace}");
        }

        return new PerformanceCast { State = CastState.TechnicalError };
    }

    private async Task ParseText(string text, PerformanceCast performanceCast, CancellationToken cancellationToken)
    {
        //detete commented
        text = Regex.Replace(text, "<!--.*?-->", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        var lines = text.Split(new[] { "<br/>", "<br>", "</p>", "<p>", "<br />" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            if (line.StartsWith(CommonTags.Phonogram, StringComparison.OrdinalIgnoreCase))
            {
                performanceCast.Cast[CommonTags.Conductor] = new List<IActor> { new PerformanceActor { Name = CommonTags.Phonogram, Url = CommonTags.NotDefinedTag } };
                continue;
            }

            string characterName = line.GetCharacterName();

            if (characterName.Length > 200 ||
                CommonTags.TechnicalTagsInCastList.Any(tag => characterName.StartsWith(tag)))
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
    }

    private static IList<IActor> GetCastInfo(IElement[] aTags)
    {
        var actors = new List<IActor>();

        foreach (var aTag in aTags)
        {
            string actorName = aTag.TextContent.Replace("&nbsp;", " ").Trim();
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