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

    static char[] Splitters = { '—', ':', '–' };
    public static string GetCharacterName(this string actorLine)
    {
        string current = actorLine.Replace("&ndash;", "-").Replace("&nbsp;", " ");

        if (Splitters.Any(splitter => current.Contains(splitter)))
        {
            return current.Split(Splitters).First().Trim();
        }

        if (current.Contains('('))
        {
            int posStart = current.IndexOf('(');
            int posEnd = current.IndexOf(')');

            string actor = posEnd > -1
                ? current.Substring(posStart + 1, posEnd - posStart - 1).Trim()
                : current.Substring(posStart + 1);

            if (!current.Substring(posEnd).Contains('('))
                return actor;
        }

        return CommonTags.Actor;
    }

    public static string GetActorName(this string actorLine)
    {
        string current = actorLine.Replace("&nbsp;", " ");

        if (Splitters.Any(splitter => current.Contains(splitter)))
        {
            return current.Split(Splitters).Last().Trim();
        }

        return null;
    }
}