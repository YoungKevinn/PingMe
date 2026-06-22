using Microsoft.JSInterop;

namespace PingMe.Frontend.Services;

public class ReadReceiptService : ApiService
{
    public ReadReceiptService(HttpClient http, IJSRuntime js) : base(http, js) { }

    public async Task MarkMessageReadAsync(int messageId)
    {
        await PostAsync($"messages/read/message/{messageId}", new { });
    }

    public async Task MarkConversationReadAsync(int? peerId, int? groupId)
    {
        var url = "messages/read/conversation";

        if (peerId.HasValue)
            url += $"?peerId={peerId.Value}";
        else if (groupId.HasValue)
            url += $"?groupId={groupId.Value}";

        await PostAsync(url, new { });
    }
}