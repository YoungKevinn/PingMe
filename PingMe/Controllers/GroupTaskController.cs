using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs;
using PingMe.Services;

namespace PingMe.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class GroupTaskController : ControllerBase
{
    private readonly IGroupTaskService _tasks;

    public GroupTaskController(IGroupTaskService tasks)
    {
        _tasks = tasks;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GroupTaskQueryDto query)
    {
        return Ok(await _tasks.GetAsync(GetUserId(), query));
    }

    [HttpGet("overdue-count")]
    public async Task<IActionResult> GetOverdueCount()
    {
        var count = await _tasks.GetOverdueCountAsync(GetUserId());
        return Ok(new { count });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var task = await _tasks.GetByIdAsync(GetUserId(), id);
        return task is null
            ? NotFound(new { message = "Task không tồn tại hoặc bạn không có quyền xem." })
            : Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGroupTaskDto request)
    {
        var result = await _tasks.CreateAsync(GetUserId(), request);
        return result.Task is null
            ? BadRequest(new { message = result.Error })
            : Ok(result.Task);
    }

    [HttpPost("from-command")]
    public async Task<IActionResult> CreateFromCommand([FromBody] CreateGroupTaskFromCommandDto request)
    {
        var result = await _tasks.CreateFromCommandAsync(GetUserId(), request.RawCommand, request.GroupId);
        if (result.Task is null)
            return BadRequest(new { message = result.Error });

        if (request.SourceMessageId.HasValue)
            await _tasks.AttachSourceMessageAsync(result.Task.Id, request.SourceMessageId.Value);

        return Ok(result.Task);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGroupTaskDto request)
    {
        var result = await _tasks.UpdateAsync(GetUserId(), id, request);
        return result.Task is null
            ? BadRequest(new { message = result.Error })
            : Ok(result.Task);
    }

    [HttpPatch("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, [FromBody] CompleteGroupTaskDto request)
    {
        var result = await _tasks.CompleteAsync(GetUserId(), id, request.IsCompleted);
        return result.Task is null
            ? BadRequest(new { message = result.Error })
            : Ok(result.Task);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _tasks.DeleteAsync(GetUserId(), id);
        return result.Success
            ? NoContent()
            : BadRequest(new { message = result.Error });
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
