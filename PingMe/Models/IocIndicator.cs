namespace PingMe.Models;

public class IocIndicator
{
    public int Id { get; set; }

    public string Type { get; set; } = "IP";

    public string Value { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Severity { get; set; } = "Medium";

    public string Status { get; set; } = "Open";

    public string Source { get; set; } = "Manual";

    public string? Tags { get; set; }

    public int CreatedByUserId { get; set; }

    public int? MessageId { get; set; }

    public int? PeerUserId { get; set; }

    public int? GroupId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }
}