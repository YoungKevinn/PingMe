using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

[Table("OneTimeSecrets")]
public class OneTimeSecret
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    [Required]
    public string SecretCipherText { get; set; } = string.Empty;

    public int CreatedByUserId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? ViewedAt { get; set; }

    public bool IsViewed { get; set; }

    public int? ViewedByUserId { get; set; }

    [MaxLength(64)]
    public string? ViewedIpHash { get; set; }

    [MaxLength(512)]
    public string? ViewedUserAgent { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CreatedByUserId))]
    public User CreatedByUser { get; set; } = null!;

    [ForeignKey(nameof(ViewedByUserId))]
    public User? ViewedByUser { get; set; }
}
