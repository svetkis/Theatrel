using System.Diagnostics;
using System.Text;
using theatrel.Interfaces.VkIntegration;
using VkNet;
using VkNet.Model;

namespace theatrel.VKIntegration;

public class VkIntegration : IVkIntegration
{
    private readonly VkApi _api = new();

    public VkIntegration()
    {
        var userId = VkSettings.VkUserId;
        if (userId == null)
        {
            Trace.TraceError("VkIntegration failed");
            return;
        }

        _api.Authorize(new ApiAuthParams
        {
            UserId = userId.Value,
            AccessToken = VkSettings.VkToken
        });
    }

    public async Task<bool> SendMessage(long vkId, string message)
    {
        try
        {
            foreach (var msgPart in SplitMessage(message))
            {
                var res = await _api.Wall.PostAsync(new WallPostParams
                {
                    OwnerId = vkId,
                    Message = msgPart,
                    FromGroup = true
                });

                await Task.Delay(400);
            }
        }
        catch (Exception e)
        {
            Trace.TraceError($"Can't post to VK {vkId} {e.Message}");
            return false;
        }
        
        return true;
    }

    private const int MaxMessageSize = 1024;
    private static string[] SplitMessage(string message)
    {
        string splitterString = $"{Environment.NewLine}{Environment.NewLine}";
        int lengthOfSplitter = splitterString.Length;

        if (message.Length < MaxMessageSize || string.IsNullOrEmpty(message))
            return new[] { message };

        string[] lines = message.Split(splitterString);
        List<StringBuilder> messages = new List<StringBuilder> { new (lines.First()) };

        foreach (var line in lines.Skip(1))
        {
            if (messages.Last().Length + line.Length >= MaxMessageSize - lengthOfSplitter)
            {
                messages.Add(new StringBuilder(line));
                continue;
            }

            messages.Last().Append(splitterString);
            messages.Last().Append(line);
        }

        return messages.Select(sb => sb.ToString()).ToArray();
    }
}