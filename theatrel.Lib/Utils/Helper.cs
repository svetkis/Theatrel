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

            int ret = 0;
            int.TryParse(value, out ret);
            return ret;
        }

        public static T Deserialize<T>(string data)
        {
            try
            {
                using (var reader = new StringReader(data))
                {
                    return (T)new XmlSerializer(typeof(T)).Deserialize(reader);
                }
            }
            catch(Exception ex)
            {
                return default(T);
            }
        }
    }
}
