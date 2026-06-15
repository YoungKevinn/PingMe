using Microsoft.JSInterop;

namespace PingMe.Frontend.Services;

public class ReactionService : ApiService
{
    public ReactionService(HttpClient http, IJSRuntime js) : base(http, js)
    {
    }

    public async Task<bool> AddReactionAsync(int messageId, string emoji)
    {
        emoji = NormalizeEmoji(emoji);

        if (messageId <= 0 || string.IsNullOrWhiteSpace(emoji))
            return false;

        try
        {
            var response = await PostAsync($"messages/{messageId}/reactions", new
            {
                Emoji = emoji
            });

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ReactAsync(int messageId, string emoji)
    {
        return await AddReactionAsync(messageId, emoji);
    }

    public async Task<bool> RemoveReactionAsync(int messageId, string emoji)
    {
        emoji = NormalizeEmoji(emoji);

        if (messageId <= 0 || string.IsNullOrWhiteSpace(emoji))
            return false;

        try
        {
            var url = $"messages/{messageId}/reactions?emoji={Uri.EscapeDataString(emoji)}";
            var response = await DeleteAsync(url);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ToggleReactionAsync(int messageId, string emoji, bool alreadyReacted)
    {
        return alreadyReacted
            ? await RemoveReactionAsync(messageId, emoji)
            : await AddReactionAsync(messageId, emoji);
    }

    private static string NormalizeEmoji(string? emoji)
    {
        emoji = emoji?.Trim() ?? string.Empty;

        if (emoji.Length > 10)
            emoji = emoji[..10];

        return emoji;
    }
}