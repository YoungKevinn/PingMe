using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs.Snippet;
using PingMe.Models;
using System.Text.RegularExpressions;

namespace PingMe.Services;

public interface ISnippetService
{
    Task<SnippetResponse> CreateSnippetAsync(int userId, CreateSnippetRequest request);

    Task<(SnippetResponse? Snippet, string? Error)> UpdateSnippetAsync(
        int snippetId,
        int userId,
        UpdateSnippetRequest request);

    Task<SnippetResponse?> GetByTokenAsync(
        string token,
        int? currentUserId,
        string? ipAddress,
        string? userAgent);

    Task<List<SnippetResponse>> GetUserSnippetsAsync(int userId);

    Task<List<SnippetResponse>> SearchSnippetsAsync(
        int userId,
        SnippetSearchRequest request);

    Task<(bool Success, string? Error)> RevokeSnippetAsync(int snippetId, int userId);

    Task<(bool Success, string? Error)> RestoreSnippetAsync(int snippetId, int userId);

    Task<(bool Success, string? Error)> DeleteSnippetAsync(int snippetId, int userId);
}

public class SnippetService : ISnippetService
{
    private readonly AppDbContext _db;

    public SnippetService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SnippetResponse> CreateSnippetAsync(int userId, CreateSnippetRequest request)
    {
        var normalized = NormalizeSnippetInput(request.Content);

        var languageFromRequest = NormalizeOptionalLanguageName(request.Language);
        var languageFromFence = NormalizeOptionalLanguageName(normalized.Language);

        var language = !string.IsNullOrWhiteSpace(languageFromFence)
            ? languageFromFence
            : !string.IsNullOrWhiteSpace(languageFromRequest) && languageFromRequest != "plaintext"
                ? languageFromRequest
                : DetectLanguage(normalized.Content);

        var title = string.IsNullOrWhiteSpace(request.Title)
            ? GenerateTitle(language)
            : request.Title.Trim();

        var snippet = new CodeSnippet
        {
            UserId = userId,
            MessageId = request.MessageId,
            Title = title,
            Content = normalized.Content,
            Language = language,
            ShareToken = Guid.NewGuid().ToString("N"),
            ExpiresAt = CalculateExpiresAt(request.ExpirationOption),
            IsRevoked = false,
            AccessCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.CodeSnippets.Add(snippet);
        await _db.SaveChangesAsync();

        var user = await _db.Users.FindAsync(userId);

        return MapToResponse(snippet, user?.DisplayName ?? "Unknown");
    }

    public async Task<(SnippetResponse? Snippet, string? Error)> UpdateSnippetAsync(
        int snippetId,
        int userId,
        UpdateSnippetRequest request)
    {
        var snippet = await _db.CodeSnippets
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == snippetId);

        if (snippet is null)
            return (null, "Snippet không tồn tại.");

        if (snippet.UserId != userId)
            return (null, "Không có quyền chỉnh sửa snippet này.");

        if (string.IsNullOrWhiteSpace(request.Content))
            return (null, "Nội dung snippet không được để trống.");

        var normalized = NormalizeSnippetInput(request.Content);

        var languageFromRequest = NormalizeOptionalLanguageName(request.Language);
        var languageFromFence = NormalizeOptionalLanguageName(normalized.Language);

        var language = !string.IsNullOrWhiteSpace(languageFromFence)
            ? languageFromFence
            : !string.IsNullOrWhiteSpace(languageFromRequest)
                ? languageFromRequest
                : DetectLanguage(normalized.Content);

        snippet.Title = string.IsNullOrWhiteSpace(request.Title)
            ? GenerateTitle(language)
            : request.Title.Trim();

        snippet.Content = normalized.Content;
        snippet.Language = language;
        snippet.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.ExpirationOption))
        {
            snippet.ExpiresAt = CalculateExpiresAt(request.ExpirationOption);
        }

        await _db.SaveChangesAsync();

        return (MapToResponse(snippet, snippet.User?.DisplayName ?? "Unknown"), null);
    }

    public async Task<SnippetResponse?> GetByTokenAsync(
        string token,
        int? currentUserId,
        string? ipAddress,
        string? userAgent)
    {
        var snippet = await _db.CodeSnippets
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.ShareToken == token);

        if (snippet is null)
            return null;

        if (snippet.IsRevoked)
            return null;

        if (snippet.ExpiresAt.HasValue && snippet.ExpiresAt.Value < DateTime.UtcNow)
            return null;

        snippet.AccessCount++;
        snippet.LastAccessedAt = DateTime.UtcNow;

        _db.SnippetAccessLogs.Add(new SnippetAccessLog
        {
            SnippetId = snippet.Id,
            AccessedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            UserId = currentUserId
        });

        await _db.SaveChangesAsync();

        return MapToResponse(snippet, snippet.User?.DisplayName ?? "Unknown");
    }

    public async Task<List<SnippetResponse>> GetUserSnippetsAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        var displayName = user?.DisplayName ?? "Unknown";

        return await _db.CodeSnippets
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => MapToResponse(s, displayName))
            .ToListAsync();
    }

    public async Task<List<SnippetResponse>> SearchSnippetsAsync(
        int userId,
        SnippetSearchRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        var displayName = user?.DisplayName ?? "Unknown";

        var query = _db.CodeSnippets
            .Where(s => s.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            query = query.Where(s => s.Title != null && s.Title.Contains(request.Title));
        }

        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            query = query.Where(s => s.Language == request.Language);
        }

        if (!string.IsNullOrWhiteSpace(request.Content))
        {
            query = query.Where(s => s.Content.Contains(request.Content));
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => MapToResponse(s, displayName))
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> RevokeSnippetAsync(int snippetId, int userId)
    {
        var snippet = await _db.CodeSnippets.FindAsync(snippetId);

        if (snippet is null)
            return (false, "Snippet không tồn tại.");

        if (snippet.UserId != userId)
            return (false, "Không có quyền thu hồi snippet này.");

        if (snippet.IsRevoked)
            return (false, "Snippet đã bị thu hồi trước đó.");

        snippet.IsRevoked = true;
        snippet.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RestoreSnippetAsync(int snippetId, int userId)
    {
        var snippet = await _db.CodeSnippets.FindAsync(snippetId);

        if (snippet is null)
            return (false, "Snippet không tồn tại.");

        if (snippet.UserId != userId)
            return (false, "Không có quyền khôi phục snippet này.");

        if (!snippet.IsRevoked)
            return (false, "Snippet hiện chưa bị thu hồi.");

        snippet.IsRevoked = false;
        snippet.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteSnippetAsync(int snippetId, int userId)
    {
        var snippet = await _db.CodeSnippets.FindAsync(snippetId);

        if (snippet is null)
            return (false, "Snippet không tồn tại.");

        if (snippet.UserId != userId)
            return (false, "Không có quyền xóa snippet này.");

        _db.CodeSnippets.Remove(snippet);

        await _db.SaveChangesAsync();

        return (true, null);
    }

    private static (string Content, string? Language) NormalizeSnippetInput(string? input)
    {
        var content = input?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(content))
            return (string.Empty, null);

        var match = Regex.Match(
            content,
            @"^\s*```(?<lang>[a-zA-Z0-9#+\-.]*)\s*\r?\n(?<code>[\s\S]*?)\r?\n?```\s*$",
            RegexOptions.Multiline);

        if (!match.Success)
            return (content, null);

        var language = match.Groups["lang"].Value.Trim();
        var code = match.Groups["code"].Value.Trim('\r', '\n');

        return (code, string.IsNullOrWhiteSpace(language) ? null : language);
    }

    private static string? NormalizeOptionalLanguageName(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return null;

        return NormalizeLanguageName(language);
    }

    private static string NormalizeLanguageName(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return "plaintext";

        return language.Trim().ToLowerInvariant() switch
        {
            "c#" => "csharp",
            "cs" => "csharp",
            "js" => "javascript",
            "ts" => "typescript",
            "py" => "python",
            "sh" => "bash",
            "shell" => "bash",
            "ps" => "powershell",
            "ps1" => "powershell",
            "md" => "markdown",
            "yml" => "yaml",
            "html" => "html",
            "plain" => "plaintext",
            "text" => "plaintext",
            var value => value
        };
    }

    private static DateTime? CalculateExpiresAt(string? option)
    {
        return option switch
        {
            "1h" => DateTime.UtcNow.AddHours(1),
            "1d" => DateTime.UtcNow.AddDays(1),
            "7d" => DateTime.UtcNow.AddDays(7),
            "30d" => DateTime.UtcNow.AddDays(30),
            "never" => null,
            _ => null
        };
    }

    private static string DetectLanguage(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "plaintext";

        if (content.Contains("using Microsoft", StringComparison.OrdinalIgnoreCase)
            || content.Contains("namespace ", StringComparison.OrdinalIgnoreCase)
            || content.Contains("public class ", StringComparison.OrdinalIgnoreCase))
        {
            return "csharp";
        }

        if (content.Contains("import ", StringComparison.OrdinalIgnoreCase)
            && content.Contains(" from ", StringComparison.OrdinalIgnoreCase))
        {
            return "typescript";
        }

        if (content.Contains("function ", StringComparison.OrdinalIgnoreCase)
            || content.Contains("const ", StringComparison.OrdinalIgnoreCase)
            || content.Contains("=>", StringComparison.OrdinalIgnoreCase))
        {
            return "javascript";
        }

        if (content.Contains("<html", StringComparison.OrdinalIgnoreCase)
            || content.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
            || content.Contains("<div", StringComparison.OrdinalIgnoreCase))
        {
            return "html";
        }

        if (content.Contains("SELECT ", StringComparison.OrdinalIgnoreCase)
            || content.Contains("INSERT ", StringComparison.OrdinalIgnoreCase)
            || content.Contains("UPDATE ", StringComparison.OrdinalIgnoreCase)
            || content.Contains("DELETE ", StringComparison.OrdinalIgnoreCase))
        {
            return "sql";
        }

        if (content.Contains("def ", StringComparison.OrdinalIgnoreCase)
            || content.Contains("print(", StringComparison.OrdinalIgnoreCase))
        {
            return "python";
        }

        if (content.Contains("package main", StringComparison.OrdinalIgnoreCase)
            || content.Contains("func ", StringComparison.OrdinalIgnoreCase))
        {
            return "go";
        }

        if (content.Contains("fn ", StringComparison.OrdinalIgnoreCase)
            && content.Contains("let ", StringComparison.OrdinalIgnoreCase))
        {
            return "rust";
        }

        return "plaintext";
    }

    private static string GenerateTitle(string language)
    {
        return $"{language} snippet - {DateTime.Now:HH:mm dd/MM}";
    }

    private static SnippetResponse MapToResponse(CodeSnippet s, string displayName)
    {
        return new SnippetResponse
        {
            Id = s.Id,
            UserId = s.UserId,
            CreatorDisplayName = displayName,
            MessageId = s.MessageId,
            Title = s.Title,
            Content = s.Content,
            Language = s.Language,
            ShareToken = s.ShareToken,
            ShareUrl = $"/snippets/share/{s.ShareToken}",
            CreatedAt = s.CreatedAt,
            ExpiresAt = s.ExpiresAt,
            IsRevoked = s.IsRevoked,
            AccessCount = s.AccessCount,
            LastAccessedAt = s.LastAccessedAt
        };
    }
}