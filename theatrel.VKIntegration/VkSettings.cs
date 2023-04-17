namespace theatrel.VKIntegration;

public static class VkSettings
{
    public static string? VkToken => Environment.GetEnvironmentVariable("TheatrelVkToken");

    public static long? VkUserId
    {
        get
        {
            string id = Environment.GetEnvironmentVariable("TheatrelVkUserId");

            return string.IsNullOrWhiteSpace(id) ? null : long.Parse(id);
        }
    }
}