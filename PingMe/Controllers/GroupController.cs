using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs.Group;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/groups")]
[Authorize]
public class GroupController : ControllerBase
{
    private readonly IGroupService _group;
    public GroupController(IGroupService group) => _group = group;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGroupRequest request)
    {
        var (success, error, group) = await _group.CreateGroupAsync(GetUserId(), request);
        if (!success) return BadRequest(new { message = error });
        return Ok(group);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyGroups()
    {
        var groups = await _group.GetUserGroupsAsync(GetUserId());
        return Ok(groups);
    }

    [HttpGet("{groupId:int}")]
    public async Task<IActionResult> GetGroup(int groupId)
    {
        var group = await _group.GetGroupAsync(groupId, GetUserId());
        return group is null
            ? StatusCode(StatusCodes.Status403Forbidden, new { message = "Bạn không còn là thành viên của nhóm này." })
            : Ok(group);
    }

    [HttpPut("{groupId:int}")]
    public async Task<IActionResult> Update(int groupId, [FromBody] UpdateGroupRequest request)
    {
        var (success, error) = await _group.UpdateGroupAsync(groupId, GetUserId(), request);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpDelete("{groupId:int}")]
    public async Task<IActionResult> Delete(int groupId)
    {
        var (success, error) = await _group.DeleteGroupAsync(groupId, GetUserId());
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpPost("{groupId:int}/members")]
    public async Task<IActionResult> AddMember(int groupId, [FromBody] AddMemberRequest request)
    {
        var (success, error) = await _group.AddMemberAsync(groupId, GetUserId(), request.UserId);
        if (!success) return ToGroupError(error);
        return NoContent();
    }

    [HttpDelete("{groupId:int}/members/{userId:int}")]
    public async Task<IActionResult> KickMember(int groupId, int userId)
    {
        var (success, error) = await _group.KickMemberAsync(groupId, GetUserId(), userId);
        if (!success) return ToGroupError(error);
        return NoContent();
    }

    [HttpPatch("{groupId:int}/members/{userId:int}/role")]
    public async Task<IActionResult> UpdateRole(int groupId, int userId, [FromBody] UpdateMemberRoleRequest request)
    {
        var (success, error) = await _group.UpdateMemberRoleAsync(groupId, GetUserId(), userId, request.Role);
        if (!success) return ToGroupError(error);
        return NoContent();
    }

    [HttpPost("{groupId:int}/leave")]
    public async Task<IActionResult> Leave(int groupId)
    {
        var (success, error) = await _group.LeaveGroupAsync(groupId, GetUserId());
        if (!success) return ToGroupError(error);
        return NoContent();
    }

    [HttpPost("{groupId:int}/avatar")]
    public async Task<IActionResult> UploadAvatar(int groupId, [FromForm] IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "File ảnh không hợp lệ." });

        var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var (success, error, url) = await _group.UploadGroupAvatarAsync(groupId, GetUserId(), file, webRoot);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { avatarUrl = url });
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    private IActionResult ToGroupError(string? error)
    {
        if (string.Equals(error, "Bạn không còn là thành viên của nhóm này.", StringComparison.Ordinal))
            return StatusCode(StatusCodes.Status403Forbidden, new { message = error });

        return BadRequest(new { message = error });
    }
}
