using System;
using System.Linq;
using AngleSharp.Dom;
using theatrel.Common;

namespace theatrel.Lib.Utils;

internal static class ParsingExtensions
{
    public static IElement GetFirstChildByProp(this IElement element, Func<IElement, string> selector, string prop)
    {
        return element.Children.FirstOrDefault(c => 0 == string.Compare(prop, selector.Invoke(c), StringComparison.Ordinal));
    }

    public static IElement GetChildByPropPath(this IElement element, Func<IElement, string> selector, params string[] props)
    {
        IElement current = element;
        foreach (var prop in props)
        {
            current = current?.GetFirstChildByProp(selector, prop);
        }

        return current;
    }

    public static IElement GetBody(this IDocument document) => document.ChildNodes.OfType<IElement>().First();

    public static string GetCharacterName(this string actorLine)
    {
        return actorLine.Contains('—') || actorLine.Contains(':') || actorLine.Contains('–')
            ? actorLine.Split('—', ':', '–').First().Replace("&nbsp;", " ").Trim()
            : CommonTags.Actor;
    }
}