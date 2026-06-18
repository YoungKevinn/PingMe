namespace PingMe.DTOs.Ioc;

public class CreateIocRequest
{
    public string Type { get; set; } = "IP";
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Severity { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public string Source { get; set; } = "Manual";
    public string? Tags { get; set; }
    public int? MessageId { get; set; }
    public int? PeerUserId { get; set; }
    public int? GroupId { get; set; }
}

public class UpdateIocRequest
{
    public string? Type { get; set; }
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string? Severity { get; set; }
    public string? Status { get; set; }
    public string? Source { get; set; }
    public string? Tags { get; set; }
}

public class CreateIocFromCommandRequest
{
    public string RawCommand { get; set; } = string.Empty;
    public int? MessageId { get; set; }
    public int? PeerUserId { get; set; }
    public int? GroupId { get; set; }
}

public class IocSearchRequest
{
    public string? Keyword { get; set; }
    public string? Type { get; set; }
    public string? Severity { get; set; }
    public string? Status { get; set; }
    public int? GroupId { get; set; }
    public int? PeerUserId { get; set; }
}

public class IocResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public int CreatedByUserId { get; set; }
    public int? MessageId { get; set; }
    public int? PeerUserId { get; set; }
    public string? PeerDisplayName { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public string ExternalUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class IocStatsResponse
{
    public int OpenCount { get; set; }
    public int InvestigatingCount { get; set; }
    public int ActiveCount { get; set; }
}
