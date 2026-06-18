using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

/// <summary>
/// Tracks active JWT sessions per device.
/// TokenHash = SHA256(raw JWT string) — never store the raw token.
/// Middleware checks IsRevoked on every request → 401 if true.
/// User can view and revoke individual sessions from their settings.
/// </summary>
[Table("UserSessions")]
public class UserSession
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    // SHA256 hash of the raw JWT — used to look up this session on each request
    [Required]
    [MaxLength(255)]
    public string TokenHash { get; set; } = string.Empty;

    // e.g. "Chrome 124 / Windows 11", parsed from User-Agent
    [MaxLength(500)]
    public string? DeviceInfo { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    // Updated on every authenticated request (or periodically via SignalR heartbeat)
    public DateTime LastActive { get; set; } = DateTime.UtcNow;

    public bool IsRevoked { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Mirrors the JWT expiry — session auto-considered invalid after this
    public DateTime ExpiresAt { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
