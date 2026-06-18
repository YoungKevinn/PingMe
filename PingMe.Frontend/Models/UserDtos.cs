namespace PingMe.Frontend.Models;

public class UpdateProfileRequest
{
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }

    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? WorkLocation { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
}

public class UserProfileResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }

    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? WorkLocation { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
    public DateTime CreatedAt { get; set; }
}