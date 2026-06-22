using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs;
using PingMe.Services;

namespace PingMe.Controllers;

[ApiController]
[Route("api/groups/{groupId}/timeline")]
[Authorize]
public class TimelineController : ControllerBase
{
    private readonly TimelineService _timelineService;

    public TimelineController(TimelineService timelineService)
    {
        _timelineService = timelineService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTimeline(int groupId, [FromQuery] TimelineQueryDto query)
    {
        var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            var timeline = await _timelineService.GetGroupTimelineAsync(currentUserId, groupId, query);
            return Ok(timeline);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }
}
