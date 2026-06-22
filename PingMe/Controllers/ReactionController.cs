using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs.Reaction;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/messages/{messageId:int}/reactions")]
[Authorize]
public class ReactionController : ControllerBase
{
    private readonly IReactionService _reaction;

    public ReactionController(IReactionService reaction)
    {
        _reaction = reaction;
    }

    [HttpPost]
    public async Task<IActionResult> Add(int messageId, [FromBody] AddReactionRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Emoji))
            return BadRequest(new { message = "Emoji không hợp lệ." });

        var (success, error) = await _reaction.AddReactionAsync(
            messageId,
            GetUserId(),
            request.Emoji);

        if (!success)
            return BadRequest(new { message = error });

        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Remove(int messageId, [FromQuery] string emoji)
    {
        if (string.IsNullOrWhiteSpace(emoji))
            return BadRequest(new { message = "Emoji không hợp lệ." });

        var (success, error) = await _reaction.RemoveReactionAsync(
            messageId,
            GetUserId(),
            emoji);

        if (!success)
            return BadRequest(new { message = error });

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
