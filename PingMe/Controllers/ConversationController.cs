using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs.Conversation;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationController : ControllerBase
{
    private readonly IConversationService _conv;
    public ConversationController(IConversationService conv) => _conv = conv;

    [HttpGet]
    public async Task<IActionResult> GetConversations()
        => Ok(await _conv.GetConversationsAsync(GetUserId()));

    [HttpPost("pin")]
    public async Task<IActionResult> Pin([FromQuery] int? peerId, [FromQuery] int? groupId)
    {
        var (success, error) = await _conv.PinConversationAsync(GetUserId(), peerId, groupId);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpDelete("pin")]
    public async Task<IActionResult> Unpin([FromQuery] int? peerId, [FromQuery] int? groupId)
    {
        var (success, error) = await _conv.UnpinConversationAsync(GetUserId(), peerId, groupId);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpPost("nickname")]
    public async Task<IActionResult> SetNickname([FromBody] SetNicknameRequest request)
    {
        await _conv.SetNicknameAsync(GetUserId(), request);
        return NoContent();
    }

    [HttpPost("background")]
    public async Task<IActionResult> SetBackground([FromBody] SetBackgroundRequest request)
    {
        await _conv.SetBackgroundAsync(GetUserId(), request);
        return NoContent();
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearHistory([FromQuery] int? peerId, [FromQuery] int? groupId)
    {
        if (groupId.HasValue)
        {
            var (success, error) = await _conv.ClearGroupHistoryAsync(GetUserId(), groupId.Value);
            if (!success) return BadRequest(new { message = error });
        }
        else if (peerId.HasValue)
        {
            var (success, error) = await _conv.ClearDmHistoryAsync(GetUserId(), peerId.Value);
            if (!success) return BadRequest(new { message = error });
        }
        else
        {
            return BadRequest(new { message = "Yêu cầu cung cấp peerId hoặc groupId." });
        }
        
        return NoContent();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
