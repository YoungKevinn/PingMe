using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

/// <summary>
/// Incoming webhook configuration for a group.
/// External services POST to /api/webhooks/{Token} with HMAC-SHA256 signature.
/// Signature is computed over the request body using Secret, sent via X-PingMe-Signature header.
/// </summary>
[Table("Webhooks")]
public class Webhook
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int GroupId { get; set; }

    [Required]
    public int CreatedByUserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    // Unique URL token: /api/webhooks/{Token}
    [Required]
    [MaxLength(255)]
    public string Token { get; set; } = string.Empty;

    // HMAC-SHA256 secret shared with the external service
    [Required]
    [MaxLength(255)]
    public string Secret { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(GroupId))]
    public Group Group { get; set; } = null!;

    [ForeignKey(nameof(CreatedByUserId))]
    public User CreatedBy { get; set; } = null!;
}
