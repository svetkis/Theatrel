using System.Linq;

namespace theatrel.TLBot.Messages
{
    internal static class MessageHelper
    {
        private static readonly string[] CharsToEscape = { "!", ".", "(", ")", "_", "*", "[", "]", "~", ">", "#", "+", "-", "=", "|", "{", "}" };
        public static string EscapeMessageForMarkupV2(this string originalMessage)
        {
            if (string.IsNullOrWhiteSpace(originalMessage))
                return string.Empty;

            return CharsToEscape.Aggregate(originalMessage,
                (current, charToReplace) => current.Replace(charToReplace, $"\\{charToReplace}"));
        }
    }
}
