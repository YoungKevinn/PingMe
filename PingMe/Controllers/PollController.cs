using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs.Poll;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/polls")]
[Authorize]
public class PollController : ControllerBase
{
    private readonly IPollService _pollService;

    public PollController(IPollService pollService)
    {
        _pollService = pollService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePollRequest request)
    {
        var (success, error, message) = await _pollService.CreatePollAsync(GetUserId(), request);
        if (!success) return BadRequest(new { message = error });
        return Ok(message);
    }

    [HttpPost("{pollId:int}/vote")]
    public async Task<IActionResult> Vote(int pollId, [FromBody] PollVoteRequest request)
    {
        var (success, error) = await _pollService.VoteAsync(pollId, GetUserId(), request);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpDelete("{pollId:int}/vote")]
    public async Task<IActionResult> Unvote(int pollId)
    {
        var (success, error) = await _pollService.UnvoteAsync(pollId, GetUserId());
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpGet("{pollId:int}")]
    public async Task<IActionResult> Get(int pollId)
    {
        var poll = await _pollService.GetPollResponseAsync(pollId, GetUserId());
        if (poll is null) return NotFound();
        return Ok(poll);
    }

    private int GetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return int.Parse(raw!);
    }
}
