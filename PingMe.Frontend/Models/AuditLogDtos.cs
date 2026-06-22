namespace PingMe.Frontend.Models;

public class AuditLogResponse
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? UserDisplayName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}