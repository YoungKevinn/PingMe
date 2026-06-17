using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs.User;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _user;
    public UserController(IUserService user) => _user = user;

    [HttpGet("{userId:int}")]
    public async Task<IActionResult> GetProfile(int userId)
    {
        var profile = await _user.GetProfileAsync(userId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("by-username/{username}")]
    public async Task<IActionResult> GetProfileByUsername(string username)
    {
        var profile = await _user.GetProfileByUsernameAsync(username);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        var (success, error, profile) = await _user.UpdateProfileAsync(userId, request);
        if (!success) return BadRequest(new { message = error });
        return Ok(profile);
    }

    [HttpPost("me/avatar")]
    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Không có file ảnh." });

        var userId = GetUserId();

        var webRoot = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot"
        );

        var (success, error, url) = await _user.UploadAvatarAsync(userId, file, webRoot);

        if (!success)
            return BadRequest(new { message = error });

        return Ok(new { avatarUrl = url });
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
