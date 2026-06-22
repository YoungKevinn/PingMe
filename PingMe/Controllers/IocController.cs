using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs.Ioc;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/iocs")]
[Authorize]
public class IocController : ControllerBase
{
    private readonly IIocService _ioc;

    public IocController(IIocService ioc)
    {
        _ioc = ioc;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIocRequest request)
    {
        var result = await _ioc.CreateAsync(GetUserId(), request);

        if (result.Ioc is null)
            return BadRequest(new { message = result.Error });

        return Ok(result.Ioc);
    }

    [HttpPost("from-command")]
    public async Task<IActionResult> CreateFromCommand([FromBody] CreateIocFromCommandRequest request)
    {
        var result = await _ioc.CreateFromCommandAsync(GetUserId(), request);

        if (result.Ioc is null)
            return BadRequest(new { message = result.Error });

        return Ok(result.Ioc);
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] IocSearchRequest request)
    {
        var result = await _ioc.SearchAsync(GetUserId(), request);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var result = await _ioc.GetStatsAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _ioc.GetByIdAsync(GetUserId(), id);

        if (result is null)
            return NotFound(new { message = "IOC không tồn tại." });

        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateIocRequest request)
    {
        var result = await _ioc.UpdateAsync(GetUserId(), id, request);

        if (result.Ioc is null)
            return BadRequest(new { message = result.Error });

        return Ok(result.Ioc);
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateIocStatusRequest request)
    {
        var result = await _ioc.UpdateStatusAsync(GetUserId(), id, request.Status);

        if (result.Ioc is null)
            return BadRequest(new { message = result.Error });

        return Ok(result.Ioc);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _ioc.DeleteAsync(GetUserId(), id);

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

public class UpdateIocStatusRequest
{
    public string Status { get; set; } = "Open";
}
