using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/reminders")]
[Authorize]
public class ReminderController : ControllerBase
{
    private readonly IReminderService _reminders;

    public ReminderController(IReminderService reminders)
    {
        _reminders = reminders;
    }

    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] ReminderQueryDto query)
    {
        var result = await _reminders.GetMineAsync(GetUserId(), query);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReminderDto request)
    {
        var result = await _reminders.CreateAsync(GetUserId(), request);
        if (result.Reminder is null)
            return BadRequest(new { message = result.Error });

        return Ok(result.Reminder);
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await _reminders.CompleteAsync(GetUserId(), id);
        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _reminders.CancelAsync(GetUserId(), id);
        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _reminders.CancelAsync(GetUserId(), id);
        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(value))
            throw new UnauthorizedAccessException("Không tìm thấy UserId trong token.");

        return int.Parse(value);
    }
}
