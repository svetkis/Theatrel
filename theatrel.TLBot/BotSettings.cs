using System;
using System.Linq;

namespace theatrel.TLBot;

public class BotSettings
{
    public static string BotToken => Environment.GetEnvironmentVariable("TheatrelBotToken");
    public static string BotProxy => Environment.GetEnvironmentVariable("TheatrelBotProxy");
    public static int BotProxyPort
    {
        get
        {
            string portString = Environment.GetEnvironmentVariable("TheatrelProxyPort");
            if (string.IsNullOrWhiteSpace(portString))
                return 0;

            return !int.TryParse(portString, out int port) ? 0 : port;
        }
    }

    public static long[] AdminIds => Environment.GetEnvironmentVariable("OwnerTelegramgId")?.Split(",")
        .Select(s => long.Parse(s.Trim())).ToArray();
}