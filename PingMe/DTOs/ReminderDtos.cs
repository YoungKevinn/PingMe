namespace PingMe.DTOs;

public class ReminderDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime RemindAtUtc { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsSent { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByDisplayName { get; set; } = string.Empty;
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public int? PeerUserId { get; set; }
    public string? PeerDisplayName { get; set; }
    public int? SourceMessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}

public class CreateReminderDto
{
    public string Text { get; set; } = string.Empty;
    public DateTime RemindAtUtc { get; set; }
    public int? GroupId { get; set; }
    public int? PeerUserId { get; set; }
    public int? SourceMessageId { get; set; }
}

public class ReminderQueryDto
{
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 30;
}
