namespace PingMe.Frontend.Models;

public class CreateWebhookRequest
{
    public int GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class WebhookResponse
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class IncomingWebhookRequest
{
    public string Content { get; set; } = string.Empty;
    public string? Username { get; set; }
}