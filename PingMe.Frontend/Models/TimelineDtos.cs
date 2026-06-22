using System;

namespace PingMe.Frontend.Models;

public class TimelineEventDto
{
    public string EventType { get; set; } = "";
    // "ioc" | "finding" | "task" | "file"

    public DateTime Timestamp { get; set; }
    public int ActorUserId { get; set; }
    public string ActorDisplayName { get; set; } = "";
    public string? ActorAvatar { get; set; }

    public int SourceId { get; set; }       // MessageId / IocId / FindingId / TaskId
    public int? RelatedId { get; set; }     // IocId / FindingId / TaskId / AttachmentId
    public int? MessageId { get; set; }
    public int? GroupId { get; set; }
    public string Title { get; set; } = ""; // preview text
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }

    // Chỉ set theo EventType, còn lại null
    public string? Severity { get; set; }   // ioc / finding
    public string? Status { get; set; }     // finding / task
    public string? IocType { get; set; }    // ioc: IP / Domain / Hash / URL / CVE
    public string? Priority { get; set; }   // task
    public string? AssignedTo { get; set; } // task
    public DateTime? DueDate { get; set; }  // task
    public string? Endpoint { get; set; }   // finding
    public string? FileName { get; set; }   // file
    public string? FileUrl { get; set; }    // file
}

public class TimelineQueryDto
{
    public string? EventType { get; set; }  // null = all
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
