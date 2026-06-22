using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PingMe.Frontend.Models;

namespace PingMe.Frontend.Services;

public class GroupService : ApiService
{
    public string? LastError { get; private set; }

    public GroupService(HttpClient http, IJSRuntime js) : base(http, js)
    {
    }

    public async Task<GroupResponse?> CreateGroupAsync(CreateGroupRequest request)
        => await PostAndGetJsonAsync<GroupResponse>("groups", request);

    public async Task<List<GroupResponse>?> GetMyGroupsAsync()
        => await GetFromJsonAsync<List<GroupResponse>>("groups");

    public async Task<GroupResponse?> GetGroupAsync(int groupId)
    {
        LastError = null;
        var response = await GetAsync($"groups/{groupId}");

        if (!response.IsSuccessStatusCode)
        {
            LastError = await ReadErrorMessageAsync(response);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<GroupResponse>();
    }

    public async Task<bool> UpdateGroupAsync(int groupId, UpdateGroupRequest request)
    {
        var response = await PutAsync($"groups/{groupId}", request);
        LastError = response.IsSuccessStatusCode ? null : await ReadErrorMessageAsync(response);
        return response.IsSuccessStatusCode;
    }

    public async Task<string?> UploadGroupAvatarAsync(int groupId, IBrowserFile file)
    {
        LastError = null;
        await AddAuthHeaderAsync();

        await using var stream = file.OpenReadStream(5 * 1024 * 1024);
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);

        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);

        content.Add(fileContent, "file", file.Name);

        var response = await _http.PostAsync($"{BaseUrl}/groups/{groupId}/avatar", content);
        if (!response.IsSuccessStatusCode)
        {
            LastError = await ReadErrorMessageAsync(response);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<GroupAvatarUploadResponse>();
        return result?.AvatarUrl;
    }

    public async Task<bool> DeleteGroupAsync(int groupId)
    {
        var response = await DeleteAsync($"groups/{groupId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AddMemberAsync(int groupId, int userId)
    {
        var response = await PostAsync($"groups/{groupId}/members", new AddMemberRequest
        {
            UserId = userId
        });

        LastError = response.IsSuccessStatusCode ? null : await ReadErrorMessageAsync(response);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> KickMemberAsync(int groupId, int userId)
    {
        var response = await DeleteAsync($"groups/{groupId}/members/{userId}");
        LastError = response.IsSuccessStatusCode ? null : await ReadErrorMessageAsync(response);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateMemberRoleAsync(int groupId, int userId, string role)
    {
        var response = await PatchAsync($"groups/{groupId}/members/{userId}/role", new UpdateMemberRoleRequest
        {
            Role = role
        });

        LastError = response.IsSuccessStatusCode ? null : await ReadErrorMessageAsync(response);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> LeaveGroupAsync(int groupId)
    {
        var response = await PostAsync($"groups/{groupId}/leave", new { });
        LastError = response.IsSuccessStatusCode ? null : await ReadErrorMessageAsync(response);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<GroupMemberResponse>> GetGroupMembersAsync(int groupId)
    {
        var group = await GetGroupAsync(groupId);
        return group?.Members ?? new List<GroupMemberResponse>();
    }

    private static async Task<string?> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            return payload?.Message;
        }
        catch
        {
            return null;
        }
    }

    private sealed class ApiErrorResponse
    {
        public string? Message { get; set; }
    }
}
