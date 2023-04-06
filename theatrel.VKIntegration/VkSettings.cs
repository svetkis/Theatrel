﻿namespace theatrel.VKIntegration;

public static class VkSettings
{
    public static string VkToken => Environment.GetEnvironmentVariable("TheatrelVkToken");
    public static long VkUserId => long.Parse(Environment.GetEnvironmentVariable("TheatrelVkUserId"));

    public static long VkGroupId => long.Parse(Environment.GetEnvironmentVariable("TheatrelVkGroupId"));
}