using System.Diagnostics;
using theatrel.Interfaces.VkIntegration;
using VkNet;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace theatrel.VKIntegration;

public class VkIntegration : IVkIntegration
{
    private readonly VkApi _api = new();

    public VkIntegration()
    {
        _api.Authorize(new ApiAuthParams
        {
            UserId = VkSettings.VkUserId,
            AccessToken = VkSettings.VkToken
        });
    }

    public async Task<bool> SendMessage(long vkId, string message)
    {
        try
        {
            var res = await _api.Wall.PostAsync(new WallPostParams
            {
                OwnerId = vkId,
                Message = message,
                FromGroup = true
            });
        }
        catch (Exception e)
        {
            Trace.TraceError($"Can't post to VK {vkId} {e.Message}");
            return false;
        }
        
        return true;
    }
}