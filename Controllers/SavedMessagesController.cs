using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/saved-messages")]
[Authorize]
public class SavedMessagesController : ControllerBase
{
    private readonly ISavedMessageService _savedMessages;

    public SavedMessagesController(ISavedMessageService savedMessages)
    {
        _savedMessages = savedMessages;
    }

    [HttpPost("{messageId:int}")]
    public async Task<IActionResult> Save(int messageId)
    {
        var result = await _savedMessages.SaveAsync(GetUserId(), messageId);
        return ToActionResult(result);
    }

    [HttpDelete("{messageId:int}")]
    public async Task<IActionResult> Unsave(int messageId)
    {
        var result = await _savedMessages.UnsaveAsync(GetUserId(), messageId);
        return ToActionResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] SavedMessageFilterDto filter)
    {
        var items = await _savedMessages.GetSavedMessagesAsync(GetUserId(), filter);
        return Ok(items);
    }

    [HttpPost("check")]
    public async Task<IActionResult> Check([FromBody] CheckSavedMessagesRequest request)
    {
        var result = await _savedMessages.CheckSavedAsync(GetUserId(), request.MessageIds);
        return Ok(result);
    }

    private IActionResult ToActionResult((bool Success, int StatusCode, string? Error) result)
    {
        if (result.Success)
            return NoContent();

        var payload = new { message = result.Error };

        return result.StatusCode switch
        {
            StatusCodes.Status404NotFound => NotFound(payload),
            StatusCodes.Status403Forbidden => StatusCode(StatusCodes.Status403Forbidden, payload),
            _ => BadRequest(payload)
        };
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
