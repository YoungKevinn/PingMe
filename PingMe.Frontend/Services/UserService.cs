using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using PingMe.Frontend.Models;
using System.Net.Http.Headers;

namespace PingMe.Frontend.Services;

public class UserService : ApiService
{
    private const long MaxAvatarSize = 3 * 1024 * 1024;

    // Backend API thật của project.
    // Vì frontend đang chạy ở https://localhost:7149 nên upload không được dùng relative URL.
    private const string ApiBaseUrl = "http://localhost:5000/api/";

    private readonly IJSRuntime _jsRuntime;

    public string? LastAvatarUploadError { get; private set; }

    public UserService(HttpClient http, IJSRuntime js) : base(http, js)
    {
        _jsRuntime = js;
    }

    public async Task<UserProfileResponse?> GetProfileAsync()
    {
        return await GetFromJsonAsync<UserProfileResponse>("auth/me");
    }

    public async Task<UserProfileResponse?> GetProfileAsync(int userId)
    {
        return await GetFromJsonAsync<UserProfileResponse>($"users/{userId}");
    }

    public async Task<UserProfileResponse?> GetProfileByUsernameAsync(string username)
    {
        return await GetFromJsonAsync<UserProfileResponse>($"users/by-username/{username}");
    }

    public async Task<bool> UpdateProfileAsync(UpdateProfileRequest request)
    {
        var response = await PutAsync("users/me", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UploadAvatarAsync(IBrowserFile file)
    {
        LastAvatarUploadError = null;

        try
        {
            if (file == null)
            {
                LastAvatarUploadError = "Không có file ảnh.";
                return false;
            }

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                LastAvatarUploadError = "File được chọn không phải ảnh.";
                return false;
            }

            if (file.Size > MaxAvatarSize)
            {
                LastAvatarUploadError = "Ảnh đại diện tối đa 3MB.";
                return false;
            }

            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "auth_token");

            if (string.IsNullOrWhiteSpace(token))
            {
                LastAvatarUploadError = "Không tìm thấy token đăng nhập. Vui lòng đăng nhập lại.";
                return false;
            }

            using var content = new MultipartFormDataContent();

            await using var fileStream = file.OpenReadStream(MaxAvatarSize);
            using var streamContent = new StreamContent(fileStream);

            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            // Backend action: UploadAvatar([FromForm] IFormFile file)
            // nên field name phải là "file".
            content.Add(streamContent, "file", file.Name);

            var uploadUrl = $"{ApiBaseUrl}users/me/avatar";

            using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
            {
                Content = content
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
                return true;

            var body = await response.Content.ReadAsStringAsync();

            LastAvatarUploadError =
                $"Upload thất bại: {(int)response.StatusCode} {response.ReasonPhrase}"
                + (string.IsNullOrWhiteSpace(body) ? "" : $" - {body}");

            return false;
        }
        catch (Exception ex)
        {
            LastAvatarUploadError = $"Lỗi upload avatar: {ex.Message}";
            return false;
        }
    }
}