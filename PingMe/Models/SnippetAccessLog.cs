using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

[Table("SnippetAccessLogs")]
public class SnippetAccessLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int SnippetId { get; set; }

    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    // Null = anonymous access via share link
    public int? UserId { get; set; }

    // Navigation
    [ForeignKey(nameof(SnippetId))]
    public CodeSnippet Snippet { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
