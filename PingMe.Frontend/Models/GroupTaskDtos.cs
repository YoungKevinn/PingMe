namespace PingMe.Frontend.Models;

public class GroupTaskQuery
{
    public int? GroupId { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public bool AssignedToMe { get; set; }
    public bool Overdue { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GroupTaskListResponse
{
    public List<GroupTaskResponse> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CreateGroupTaskRequest
{
    public int GroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AssignedToUserId { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public string Priority { get; set; } = "Medium";
}

public class UpdateGroupTaskRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? AssignedToUserId { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
}

public class CompleteGroupTaskRequest
{
    public bool IsCompleted { get; set; } = true;
}

public class CreateGroupTaskFromCommandRequest
{
    public string RawCommand { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public int? SourceMessageId { get; set; }
}

public class GroupTaskResponse
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DueAtUtc { get; set; }
    public int CreatedByUserId { get; set; }
    public string CreatedByDisplayName { get; set; } = string.Empty;
    public int? AssignedToUserId { get; set; }
    public string? AssignedToDisplayName { get; set; }
    public int? SourceMessageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class TaskAssignedEvent
{
    public int GroupId { get; set; }
    public int AssignedToUserId { get; set; }
    public GroupTaskResponse Task { get; set; } = new();
}

public class TaskMessagePayload
{
    public string Type { get; set; } = "task";
    public int TaskId { get; set; }
    public int GroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public DateTime? DueAtUtc { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? AssignedToDisplayName { get; set; }
    public string? Url { get; set; }
}
