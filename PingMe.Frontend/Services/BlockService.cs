using Microsoft.JSInterop;
using PingMe.Frontend.Models;

namespace PingMe.Frontend.Services;

public class BlockService : ApiService
{
    public BlockService(HttpClient http, IJSRuntime js) : base(http, js) { }

    public async Task<List<BlockResponse>?> GetBlockedUsersAsync()
        => await GetFromJsonAsync<List<BlockResponse>>("blocks");

    public async Task<BlockStatusResponse?> GetBlockStatusAsync(int userId)
        => await GetFromJsonAsync<BlockStatusResponse>($"blocks/{userId}/status");

    public async Task<bool> BlockUserAsync(int userId)
    {
        await AddAuthHeaderAsync();
        var r = await _http.PostAsync($"{BaseUrl}/blocks/{userId}", null);
        return r.IsSuccessStatusCode;
    }

    public async Task<bool> UnblockUserAsync(int userId)
    {
        var r = await DeleteAsync($"blocks/{userId}");
        return r.IsSuccessStatusCode;
    }
}
