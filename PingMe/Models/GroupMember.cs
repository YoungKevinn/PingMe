using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PingMe.Models;

[Table("GroupMembers")]
public class GroupMember
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int GroupId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ClearedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(GroupId))]
    public Group Group { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
