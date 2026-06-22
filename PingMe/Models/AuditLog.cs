using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

/// <summary>
/// Immutable audit trail. Never update or delete rows — append only.
/// Metadata is a JSON blob for flexible per-action context.
/// </summary>
[Table("AuditLogs")]
public class AuditLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Nullable: system-level events may not have a user
    public int? UserId { get; set; }

    // e.g. "user.login", "user.logout", "group.member.kick", "group.delete", "message.delete"
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    // JSON-serialized extra context (e.g. {"targetUserId": 42, "groupId": 7})
    public string? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
