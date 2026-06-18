namespace PingMe.Frontend.Models;

public class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<int> MemberIds { get; set; } = new();
}

public class UpdateGroupRequest
{
    public string? Name { get; set; }

    public string? Description { get; set; }
}

public class AddMemberRequest
{
    public int UserId { get; set; }
}

public class UpdateMemberRoleRequest
{
    public string Role { get; set; } = "Member";
}

public class GroupResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? AvatarUrl { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<GroupMemberResponse> Members { get; set; } = new();
}

public class GroupAvatarUploadResponse
{
    public string? AvatarUrl { get; set; }
}

public class GroupMemberKickedEvent
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public string? Message { get; set; }
}

public class GroupMemberAddedEvent
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public string? Message { get; set; }
    public string? GroupName { get; set; }
    public string? GroupAvatarUrl { get; set; }
}

public class EditGroupResult
{
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public class GroupMemberResponse
{
    public int UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public bool IsOnline { get; set; }

    public string Role { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; }
}
