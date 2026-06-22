using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

[Table("CodeSnippets")]
public class CodeSnippet
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    // The message this snippet was shared in (nullable for standalone snippets)
    public int? MessageId { get; set; }

    [MaxLength(255)]
    public string? Title { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    // Language detected by highlight.js on the frontend (e.g. "csharp", "python")
    [Required]
    [MaxLength(50)]
    public string Language { get; set; } = "plaintext";

    // Unique token used in shareable URL: /snippet/{ShareToken}
    [Required]
    [MaxLength(100)]
    public string ShareToken { get; set; } = string.Empty;

    // Phase 2: expiration support (null = never expires)
    public DateTime? ExpiresAt { get; set; }

    // Phase 2: revoke link
    public bool IsRevoked { get; set; } = false;

    // Phase 2: access tracking
    public int AccessCount { get; set; } = 0;
    public DateTime? LastAccessedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(MessageId))]
    public Message? Message { get; set; }

    public ICollection<SnippetAccessLog> AccessLogs { get; set; } = [];
}
