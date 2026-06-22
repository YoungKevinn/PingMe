using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly ISessionService _session;
    private readonly IJwtService _jwt;
    public SessionController(ISessionService session, IJwtService jwt) { _session = session; _jwt = jwt; }

    [HttpGet]
    public async Task<IActionResult> GetSessions()
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var tokenHash = _jwt.HashToken(token);
        return Ok(await _session.GetSessionsAsync(GetUserId(), tokenHash));
    }

    [HttpDelete("{sessionId:int}")]
    public async Task<IActionResult> Revoke(int sessionId)
    {
        var (success, error) = await _session.RevokeSessionAsync(sessionId, GetUserId());
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpDelete("others")]
    public async Task<IActionResult> RevokeOthers()
    {
        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        var tokenHash = _jwt.HashToken(token);
        await _session.RevokeAllOtherSessionsAsync(GetUserId(), tokenHash);
        return NoContent();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
