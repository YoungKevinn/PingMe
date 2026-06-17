using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _audit;
    public AuditLogController(IAuditLogService audit) => _audit = audit;

    /// <summary>Lấy danh sách log (filter theo userId, action, IP, thời gian)</summary>
    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int? userId,
        [FromQuery] string? action,
        [FromQuery] string? ip,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var logs = await _audit.GetLogsAsync(userId, action, ip, from, to, page, Math.Min(pageSize, 100));
        return Ok(logs);
    }

    /// <summary>Lấy log của bản thân</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyLogs(
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var logs = await _audit.GetLogsAsync(GetUserId(), action, null, from, to, page, Math.Min(pageSize, 100));
        return Ok(logs);
    }

    /// <summary>Lấy chi tiết 1 log theo ID</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var log = await _audit.GetByIdAsync(id);
        return log is null ? NotFound() : Ok(log);
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
