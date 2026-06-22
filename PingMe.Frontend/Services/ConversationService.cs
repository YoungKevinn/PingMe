using Microsoft.JSInterop;
using PingMe.Frontend.Models;

namespace PingMe.Frontend.Services;

public class ConversationService : ApiService
{
    public ConversationService(HttpClient http, IJSRuntime js) : base(http, js)
    {
    }

    public async Task<List<ConversationDto>> GetConversationsAsync()
    {
        return await GetFromJsonAsync<List<ConversationDto>>("conversations") ?? new();
    }

    public async Task<UserProfileResponse?> GetUserAsync(int userId)
    {
        return await GetFromJsonAsync<UserProfileResponse>($"users/{userId}");
    }

    public async Task<GroupResponse?> GetGroupAsync(int groupId)
    {
        return await GetFromJsonAsync<GroupResponse>($"groups/{groupId}");
    }

    public async Task<List<UserDto>> SearchUsersAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return new List<UserDto>();
        }

        var result = await GetFromJsonAsync<SearchResultResponse>(
            $"search?q={Uri.EscapeDataString(term)}&limit=10"
        );

        return result?.Users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Username,
            DisplayName = u.DisplayName,
            AvatarUrl = u.AvatarUrl,
            IsOnline = u.IsOnline
        }).ToList() ?? new List<UserDto>();
    }

    public async Task<bool> PinConversationAsync(int? peerId, int? groupId)
    {
        var response = await PostAsync($"conversations/pin?peerId={peerId}&groupId={groupId}", new { });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UnpinConversationAsync(int? peerId, int? groupId)
    {
        var response = await DeleteAsync($"conversations/pin?peerId={peerId}&groupId={groupId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SetNicknameAsync(SetNicknameRequest request)
    {
        var response = await PostAsync("conversations/nickname", request);
        return response.IsSuccessStatusCode;
    }

    public async Task SetBackgroundAsync(SetBackgroundRequest request)
    {
        await PostAsync("conversations/background", request);
    }

    public async Task<(bool Success, string? Error)> ClearHistoryAsync(int? peerId, int? groupId)
    {
        var query = groupId.HasValue ? $"?groupId={groupId}" : $"?peerId={peerId}";
        var response = await DeleteAsync($"conversations/clear{query}");
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, "Lỗi xóa đoạn chat");
    }
}
