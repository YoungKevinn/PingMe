using Microsoft.JSInterop;
using PingMe.Frontend.Models;

namespace PingMe.Frontend.Services;

public class SavedMessageService : ApiService
{
    public SavedMessageService(HttpClient http, IJSRuntime js) : base(http, js)
    {
    }

    public async Task<bool> SaveAsync(int messageId)
    {
        if (messageId <= 0)
            return false;

        try
        {
            var response = await PostAsync($"saved-messages/{messageId}", new { });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UnsaveAsync(int messageId)
    {
        if (messageId <= 0)
            return false;

        try
        {
            var response = await DeleteAsync($"saved-messages/{messageId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<SavedMessageDto>> GetSavedMessagesAsync(SavedMessageFilterDto filter)
    {
        var url = $"saved-messages?page={filter.Page}&pageSize={filter.PageSize}";

        if (!string.IsNullOrWhiteSpace(filter.Type))
            url += $"&type={Uri.EscapeDataString(filter.Type)}";

        if (filter.GroupId.HasValue)
            url += $"&groupId={filter.GroupId.Value}";

        if (filter.PeerUserId.HasValue)
            url += $"&peerUserId={filter.PeerUserId.Value}";

        return await GetFromJsonAsync<List<SavedMessageDto>>(url) ?? new List<SavedMessageDto>();
    }

    public async Task<Dictionary<int, bool>> CheckSavedAsync(IEnumerable<int> messageIds)
    {
        var ids = messageIds
            .Where(id => id > 0)
            .Distinct()
            .Take(200)
            .ToList();

        if (ids.Count == 0)
            return new Dictionary<int, bool>();

        return await PostAndGetJsonAsync<Dictionary<int, bool>>(
            "saved-messages/check",
            new CheckSavedMessagesRequest { MessageIds = ids })
            ?? new Dictionary<int, bool>();
    }
}
