using System;
using System.Collections;

namespace theatrel.TLBot
{
    public class ThSettings
    {
        private static Lazy<ThSettings> _config = new Lazy<ThSettings>(() => new ThSettings());
        private IDictionary _envVariables;

        public static ThSettings Config => _config.Value;

        private ThSettings()
        {
            _envVariables = Environment.GetEnvironmentVariables();
        }

        public string GetValue(string name) => _envVariables[name].ToString();
        public int GetIntValue(string name) => int.Parse(_envVariables[name].ToString());

        public string BotToken => GetValue("TheatrelBotToken");
        public string BotProxy => GetValue("TheatrelBotProxy");
        public int BotProxyPort => GetIntValue("TheatrelProxyPort");
    }
}
