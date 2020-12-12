using System;
using System.IO;
using System.Xml.Serialization;

namespace theatrel.Lib.Utils
{
    internal class Helper
    {
        public static int ToInt(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            int.TryParse(value, out var ret);
            return ret;
        }
    }
}
