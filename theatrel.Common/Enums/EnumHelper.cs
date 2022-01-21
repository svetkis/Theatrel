﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace theatrel.Common.Enums;

public static class EnumHelper
{
    public static string Description(this Enum value)
    {
        var attributes = value.GetType().GetField(value.ToString())
            .GetCustomAttributes(typeof(DescriptionAttribute), false);

        if (attributes.Any())
            return (attributes.First() as DescriptionAttribute).Description;

        TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
        return ti.ToTitleCase(ti.ToLower(value.ToString().Replace("_", " ")));
    }
}