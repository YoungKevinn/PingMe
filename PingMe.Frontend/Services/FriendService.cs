using Microsoft.JSInterop;
using PingMe.Frontend.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace PingMe.Frontend.Services;

public class FriendService : ApiService
{
    public FriendService(HttpClient http, IJSRuntime js) : base(http, js) { }

    public async Task<(bool Success, string? Error)> SendFriendRequestAsync(int userId)
    {
        var response = await PostAsync("friends/request", new { TargetUserId = userId });
        return await ParseResultAsync(response);
    }

    public async Task<(bool Success, string? Error)> AcceptFriendRequestAsync(int requestId)
    {
        var response = await PostAsync($"friends/accept/{requestId}", new { });
        return await ParseResultAsync(response);
    }

    public async Task<(bool Success, string? Error)> DeclineFriendRequestAsync(int requestId)
    {
        var response = await PostAsync($"friends/decline/{requestId}", new { });
        return await ParseResultAsync(response);
    }

    public async Task<(bool Success, string? Error)> CancelFriendRequestAsync(int requestId)
    {
        var response = await DeleteAsync($"friends/cancel/{requestId}");
        return await ParseResultAsync(response);
    }

    public async Task<(bool Success, string? Error)> RemoveFriendAsync(int friendId)
    {
        var response = await DeleteAsync($"friends/{friendId}");
        return await ParseResultAsync(response);
    }

    public async Task<List<FriendRequestDto>> GetPendingRequestsAsync()
    {
        return await GetFromJsonAsync<List<FriendRequestDto>>("friends/requests") ?? new();
    }

    public async Task<List<FriendRequestDto>> GetSentRequestsAsync()
    {
        return await GetFromJsonAsync<List<FriendRequestDto>>("friends/sent") ?? new();
    }

    public async Task<List<FriendResponseDto>> GetFriendsAsync()
    {
        return await GetFromJsonAsync<List<FriendResponseDto>>("friends") ?? new();
    }

    public async Task<bool> AreFriendsAsync(int otherId)
    {
        var result = await GetFromJsonAsync<AreFriendsResult>($"friends/check/{otherId}");
        return result?.AreFriends ?? false;
    }

    private static async Task<(bool Success, string? Error)> ParseResultAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return (true, null);

        try
        {
            var body = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body))
                return (false, $"Lỗi {(int)response.StatusCode}");

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                return (false, msg.GetString());

            return (false, body);
        }
        catch
        {
            return (false, $"Lỗi {(int)response.StatusCode}");
        }
    }

    private class AreFriendsResult
    {
        public bool AreFriends { get; set; }
    }
}
