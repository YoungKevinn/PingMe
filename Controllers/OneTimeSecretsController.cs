using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/one-time-secrets")]
public class OneTimeSecretsController : ControllerBase
{
    private readonly IOneTimeSecretService _secrets;

    public OneTimeSecretsController(IOneTimeSecretService secrets)
    {
        _secrets = secrets;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOneTimeSecretRequest request)
    {
        var result = await _secrets.CreateAsync(GetUserId(), request, GetFrontendBaseUrl());

        if (result.Secret is null)
            return BadRequest(new { message = result.Error });

        return Ok(result.Secret);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var items = await _secrets.GetMineAsync(GetUserId());
        return Ok(items);
    }

    [Authorize]
    [HttpPost("{id:int}/revoke")]
    public async Task<IActionResult> Revoke(int id)
    {
        var result = await _secrets.RevokeAsync(GetUserId(), id);

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Đã thu hồi secret." });
    }

    [AllowAnonymous]
    [HttpGet("open/{token}")]
    public async Task<IActionResult> Open(string token)
    {
        var result = await _secrets.ViewAsync(
            token,
            TryGetUserId(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        if (result.Secret is null)
            return BadRequest(new { message = result.Error });

        return Ok(result.Secret);
    }

    private string GetFrontendBaseUrl()
    {
        var origin = Request.Headers.Origin.ToString();
        if (!string.IsNullOrWhiteSpace(origin))
            return origin;

        return $"{Request.Scheme}://{Request.Host}";
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(value))
            throw new UnauthorizedAccessException("Không tìm thấy UserId trong token.");

        return int.Parse(value);
    }

    private int? TryGetUserId()
    {
        if (User.Identity?.IsAuthenticated != true)
            return null;

        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

        return int.TryParse(value, out var userId) ? userId : null;
    }
}
