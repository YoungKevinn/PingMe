namespace PingMe.DTOs.Search;

public class GlobalSearchRequestDto
{
    public string? Keyword { get; set; }
    public string? Type { get; set; } = "all";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? SenderId { get; set; }
    public int? GroupId { get; set; }
    public int? PeerUserId { get; set; }
    public string? Severity { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GlobalSearchResponseDto
{
    public List<GlobalSearchResultDto> Items { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public Dictionary<string, int>? TypeCounts { get; set; }
}

public class GlobalSearchResultDto
{
    public string Type { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Snippet { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? Url { get; set; }
    public string? AvatarUrl { get; set; }
    public int? MessageId { get; set; }
    public int? GroupId { get; set; }
    public int? PeerUserId { get; set; }
    public string? GroupName { get; set; }
    public string? PeerName { get; set; }
    public string? PeerAvatarUrl { get; set; }
    public string? SenderName { get; set; }
    public string? SenderAvatarUrl { get; set; }
    public string? Severity { get; set; }
    public string? Status { get; set; }
    public string? AffectedEndpoint { get; set; }
    public string? HttpMethod { get; set; }
    public string? PayloadPreview { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public string? FileContentType { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public class SearchResultResponse
{
    public List<UserSearchResult> Users { get; set; } = [];
    public List<MessageSearchResult> Messages { get; set; } = [];
}

public class UserSearchResult
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsOnline { get; set; }

    public string? JobTitle { get; set; }
    public string? Department { get; set; }
}

public class MessageSearchResult
{
    public int Id { get; set; }
    public string? Content { get; set; }

    public int SenderId { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;

    public int? GroupId { get; set; }
    public int? ReceiverId { get; set; }

    public string MessageType { get; set; } = string.Empty;
    public string? AttachmentFileName { get; set; }

    public DateTime CreatedAt { get; set; }
}
