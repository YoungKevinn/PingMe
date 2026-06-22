using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

[Table("Polls")]
public class Poll
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int MessageId { get; set; }

    public bool AllowMultiple { get; set; } = false;

    public DateTime? EndsAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(MessageId))]
    public Message Message { get; set; } = null!;

    public ICollection<PollOption> Options { get; set; } = [];
}
