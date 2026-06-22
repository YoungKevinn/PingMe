using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

[Table("PollOptions")]
public class PollOption
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int PollId { get; set; }

    [Required]
    [MaxLength(300)]
    public string Text { get; set; } = string.Empty;

    public int Order { get; set; } = 0;

    [ForeignKey(nameof(PollId))]
    public Poll Poll { get; set; } = null!;

    public ICollection<PollVote> Votes { get; set; } = [];
}
