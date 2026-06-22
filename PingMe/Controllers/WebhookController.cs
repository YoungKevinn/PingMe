using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs.Webhook;
using PingMe.Services;
using System.Security.Claims;
using System.Text;

namespace PingMe.Controllers;

[ApiController]
[Route("api/webhooks")]
[Authorize]
public class WebhookController : ControllerBase
{
    private readonly IWebhookService _webhook;
    public WebhookController(IWebhookService webhook) => _webhook = webhook;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWebhookRequest request)
        => Ok(await _webhook.CreateWebhookAsync(GetUserId(), request));

    [HttpGet("group/{groupId:int}")]
    public async Task<IActionResult> GetGroupWebhooks(int groupId)
        => Ok(await _webhook.GetGroupWebhooksAsync(groupId, GetUserId()));

    [HttpDelete("{webhookId:int}")]
    public async Task<IActionResult> Delete(int webhookId)
    {
        var (success, error) = await _webhook.DeleteWebhookAsync(webhookId, GetUserId());
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpPatch("{webhookId:int}/toggle")]
    public async Task<IActionResult> Toggle(int webhookId, [FromQuery] bool active)
    {
        var (success, error) = await _webhook.ToggleWebhookAsync(webhookId, GetUserId(), active);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("incoming/{token}")]
    public async Task<IActionResult> Incoming(string token, [FromBody] IncomingWebhookRequest request)
    {
        var signature = Request.Headers["X-PingMe-Signature"].FirstOrDefault() ?? string.Empty;
        Request.Body.Seek(0, System.IO.SeekOrigin.Begin);
        using var reader = new System.IO.StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync();
        var (success, error) = await _webhook.ProcessIncomingAsync(token, signature, rawBody, request);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Message sent." });
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
