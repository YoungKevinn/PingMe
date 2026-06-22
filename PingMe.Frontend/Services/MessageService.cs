using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using PingMe.Frontend.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PingMe.Frontend.Services;

public class MessageService : ApiService
{
    private const long MaxUploadBytes = 25 * 1024 * 1024;
    private const int MaxTextMessageLength = 4000;
    private const int MaxPinnedMessagesPerConversation = 5;
    public string? LastError { get; private set; }
    public MessageService(HttpClient http, IJSRuntime js) : base(http, js) { }

    public async Task<MessageResponse?> SendMessageAsync(SendMessageRequest request)
    {
        LastError = null;
        await AddAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync($"{BaseUrl}/messages", request);

        if (!response.IsSuccessStatusCode)
        {
            LastError = await ReadErrorMessageAsync(response);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<MessageResponse>();
    }

    public async Task<MessageResponse?> SendDmAsync(int receiverId, string content, int? replyToMessageId = null)
    {
        return await SendMessageAsync(new SendMessageRequest
        {
            ReceiverId = receiverId,
            Content = content,
            MessageType = "Text",
            ReplyToMessageId = replyToMessageId
        });
    }

    public async Task<MessageResponse?> SendGroupAsync(int groupId, string content, int? replyToMessageId = null)
    {
        return await SendMessageAsync(new SendMessageRequest
        {
            GroupId = groupId,
            Content = content,
            MessageType = "Text",
            ReplyToMessageId = replyToMessageId
        });
    }

    public async Task<MessageResponse?> UploadDmAsync(int receiverId, IBrowserFile file, int? replyToMessageId = null)
    {
        return await UploadAsync(file, receiverId: receiverId, groupId: null, replyToMessageId: replyToMessageId);
    }

    public async Task<MessageResponse?> UploadGroupAsync(int groupId, IBrowserFile file, int? replyToMessageId = null)
    {
        return await UploadAsync(file, receiverId: null, groupId: groupId, replyToMessageId: replyToMessageId);
    }

    private async Task<MessageResponse?> UploadAsync(IBrowserFile file, int? receiverId, int? groupId, int? replyToMessageId)
    {
        try
        {
            await AddAuthHeaderAsync();

            using var form = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream(MaxUploadBytes);
            using var fileContent = new StreamContent(stream);

            if (!string.IsNullOrWhiteSpace(file.ContentType))
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(file.ContentType);

            form.Add(fileContent, "file", file.Name);

            if (receiverId.HasValue)
                form.Add(new StringContent(receiverId.Value.ToString()), "receiverId");

            if (groupId.HasValue)
                form.Add(new StringContent(groupId.Value.ToString()), "groupId");

            if (replyToMessageId.HasValue)
                form.Add(new StringContent(replyToMessageId.Value.ToString()), "replyToMessageId");

            var response = await _http.PostAsync($"{BaseUrl}/messages/upload", form);
            if (!response.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(response);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<MessageResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<MessageResponse?> UploadAudioAsync(byte[] fileBytes, string contentType, int? receiverId, int? groupId, int? replyToMessageId = null)
    {
        try
        {
            await AddAuthHeaderAsync();

            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

            form.Add(fileContent, "file", $"voice_message_{DateTime.Now:yyyyMMdd_HHmmss}.webm");

            if (receiverId.HasValue)
                form.Add(new StringContent(receiverId.Value.ToString()), "receiverId");

            if (groupId.HasValue)
                form.Add(new StringContent(groupId.Value.ToString()), "groupId");

            if (replyToMessageId.HasValue)
                form.Add(new StringContent(replyToMessageId.Value.ToString()), "replyToMessageId");

            var response = await _http.PostAsync($"{BaseUrl}/messages/upload", form);
            if (!response.IsSuccessStatusCode)
            {
                LastError = await ReadErrorMessageAsync(response);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<MessageResponse>();
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<MessageResponse>?> GetDmMessagesAsync(int peerId, int? before = null, int limit = 30)
    {
        var url = $"messages/dm/{peerId}?limit={limit}";
        if (before.HasValue) url += $"&before={before}";
        return await GetFromJsonAsync<List<MessageResponse>>(url);
    }

    public async Task<List<MessageResponse>?> GetGroupMessagesAsync(int groupId, int? before = null, int limit = 30)
    {
        var url = $"messages/group/{groupId}?limit={limit}";
        if (before.HasValue) url += $"&before={before}";
        LastError = null;
        await AddAuthHeaderAsync();
        var response = await _http.GetAsync($"{BaseUrl}/{url}");

        if (!response.IsSuccessStatusCode)
        {
            LastError = await ReadErrorMessageAsync(response);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<List<MessageResponse>>();
    }

    public async Task<List<MessageResponse>?> GetMessageContextAsync(int messageId, int takeBefore = 20, int takeAfter = 20)
    {
        var url = $"messages/{messageId}/context?takeBefore={takeBefore}&takeAfter={takeAfter}";
        return await GetFromJsonAsync<List<MessageResponse>>(url);
    }

    public async Task<List<MessageResponse>?> GetPinnedMessagesAsync(int? groupId = null, int? peerId = null)
    {
        var url = "messages/pinned";
        if (groupId.HasValue) url += $"?groupId={groupId}";
        else if (peerId.HasValue) url += $"?peerId={peerId}";
        return await GetFromJsonAsync<List<MessageResponse>>(url);
    }

    public async Task<bool> EditMessageAsync(int messageId, string content)
    {
        var response = await PutAsync($"messages/{messageId}", new { content });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteMessageAsync(int messageId)
    {
        var response = await DeleteAsync($"messages/{messageId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PinMessageAsync(int messageId, bool isPinned)
    {
        var response = await PatchAsync($"messages/{messageId}/pin", new { isPinned });
        return response.IsSuccessStatusCode;
    }
    public async Task<List<ConversationAttachmentResponse>?> GetConversationAttachmentsAsync(
    int? groupId,
    int? peerId,
    string type = "all",
    int limit = 80)
    {
        var url = $"messages/attachments?type={Uri.EscapeDataString(type)}&limit={limit}";

        if (groupId.HasValue)
            url += $"&groupId={groupId.Value}";

        if (peerId.HasValue)
            url += $"&peerId={peerId.Value}";

        return await GetFromJsonAsync<List<ConversationAttachmentResponse>>(url);
    }
    public async Task<MessageResponse?> ForwardMessageAsync(int messageId, int? receiverId, int? groupId)
    {
        var url = $"messages/{messageId}/forward";
        if (receiverId.HasValue) url += $"?receiverId={receiverId}";
        else if (groupId.HasValue) url += $"?groupId={groupId}";

        await AddAuthHeaderAsync();
        var response = await _http.PostAsync($"{BaseUrl}/{url}", null);

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<MessageResponse>();

        return null;
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

    public async Task<List<EditHistoryDto>> GetEditHistoryAsync(int messageId)
    {
        return await GetFromJsonAsync<List<EditHistoryDto>>($"messages/{messageId}/history") ?? new();
    }
}
