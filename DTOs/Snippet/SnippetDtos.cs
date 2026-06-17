namespace PingMe.DTOs.Snippet;

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

    // Nếu null hoặc rỗng thì giữ nguyên thời hạn cũ.
    // Nếu có giá trị: "1h" | "1d" | "7d" | "30d" | "never"
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
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
}
