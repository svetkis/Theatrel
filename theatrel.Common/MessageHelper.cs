using System;
using System.Linq;
using theatrel.Common.Enums;

namespace theatrel.Common
{
    public static class MessageHelper
    {
        public static string GetTrackingChangesDescription(this int input)
        {
            return string.Join(" ", Enum.GetValues(typeof(ReasonOfChanges))
                .OfType<ReasonOfChanges>()
                .Skip(1)
                .Where(reasonOfChange => ((ReasonOfChanges)input).HasFlag(reasonOfChange))
                .Select(reasonOfChange => reasonOfChange.Description()));
        }
    }
}
