namespace PingMe.Frontend.Models;

public class EditHistoryDto
{
    public int Id { get; set; }
    public string OldContent { get; set; } = string.Empty;
    public string NewContent { get; set; } = string.Empty;
    public DateTime EditedAt { get; set; }
}