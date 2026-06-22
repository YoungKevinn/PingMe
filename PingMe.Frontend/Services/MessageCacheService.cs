using Microsoft.JSInterop;
using PingMe.Frontend.Models;

namespace PingMe.Frontend.Services;

public class MessageCacheService
{
    private readonly IJSRuntime _js;

    public MessageCacheService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<List<MessageResponse>?> GetCachedMessagesAsync(string conversationKey)
    {
        try
        {
            return await _js.InvokeAsync<List<MessageResponse>?>(
                "pingmeCache.getMessages",
                conversationKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task SetCachedMessagesAsync(string conversationKey, List<MessageResponse> messages)
    {
        try
        {
            await _js.InvokeVoidAsync("pingmeCache.setMessages", conversationKey, messages);
        }
        catch
        {
        }
    }

    public async Task ClearCachedMessagesAsync(string conversationKey)
    {
        try
        {
            await _js.InvokeVoidAsync("pingmeCache.clearMessages", conversationKey);
        }
        catch
        {
        }
    }
}
