using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/messages/read")]
[Authorize]
public class ReadReceiptController : ControllerBase
{
    private readonly IReadReceiptService _receipt;

    public ReadReceiptController(IReadReceiptService receipt)
    {
        _receipt = receipt;
    }

    [HttpPost("message/{messageId:int}")]
    public async Task<IActionResult> MarkMessageRead(int messageId)
    {
        await _receipt.MarkAsReadAsync(messageId, GetUserId());
        return NoContent();
    }

    [HttpPost("conversation")]
    public async Task<IActionResult> MarkConversationRead([FromQuery] int? peerId, [FromQuery] int? groupId)
    {
        await _receipt.MarkConversationAsReadAsync(GetUserId(), peerId, groupId);
        return NoContent();
    }

    private int GetUserId()
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(rawId))
            throw new UnauthorizedAccessException("Không tìm thấy user id trong token.");

        return int.Parse(rawId);
    }
}