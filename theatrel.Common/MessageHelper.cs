using System;
using System.Linq;
using theatrel.Common.Enums;

namespace theatrel.Common
{
    public static class MessageHelper
    {
        public static string GetTrackingChangesDescription(this int input)
        {
            return string.Join(", ", Enum.GetValues(typeof(ReasonOfChanges))
                .OfType<ReasonOfChanges>()
                .Skip(1)
                .Where(reasonOfChange => ((ReasonOfChanges)input).HasFlag(reasonOfChange))
                .Select(reasonOfChange => reasonOfChange.Description()));
        }

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
