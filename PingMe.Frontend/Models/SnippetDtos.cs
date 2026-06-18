namespace PingMe.Frontend.Models;

public class CreateSnippetRequest
{
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Language { get; set; } = "plaintext";
    public int? MessageId { get; set; }

    // "1h" | "1d" | "7d" | "never"
    public string ExpirationOption { get; set; } = "never";
}
public class UpdateSnippetRequest
{
    public string? Title { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? Language { get; set; }

    public string? ExpirationOption { get; set; }
}
public class SnippetSearchRequest
{
    public string? Title { get; set; }
    public string? Language { get; set; }
    public string? Content { get; set; }
}

public class SnippetResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CreatorDisplayName { get; set; } = string.Empty;
    public int? MessageId { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string ShareToken { get; set; } = string.Empty;
    public string ShareUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public int AccessCount { get; set; }
    public DateTime? LastAccessedAt { get; set; }

    // Computed helpers for UI
    public bool IsExpired  => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
    public bool IsActive   => !IsRevoked && !IsExpired;

    /// Human-readable expiry label (e.g. "Còn 6 giờ", "Hết hạn", "Không hết hạn")
    public string ExpiryLabel
    {
        get
        {
            if (IsRevoked) return "Đã thu hồi";
            if (!ExpiresAt.HasValue) return "Không hết hạn";
            var remaining = ExpiresAt.Value - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) return "Đã hết hạn";
            if (remaining.TotalHours < 1) return $"Còn {(int)remaining.TotalMinutes} phút";
            if (remaining.TotalHours < 24) return $"Còn {(int)remaining.TotalHours} giờ";
            return $"Còn {(int)remaining.TotalDays} ngày";
        }
    }
}
