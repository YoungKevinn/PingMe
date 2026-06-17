using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/blocks")]
[Authorize]
public class BlockController : ControllerBase
{
    private readonly IBlockService _block;
    public BlockController(IBlockService block) => _block = block;

    [HttpGet]
    public async Task<IActionResult> GetBlocked()
        => Ok(await _block.GetBlockedUsersAsync(GetUserId()));

    [HttpGet("{userId:int}/status")]
    public async Task<IActionResult> GetStatus(int userId)
    {
        var (isBlockedByMe, hasBlockedMe) = await _block.GetBlockStatusAsync(GetUserId(), userId);
        return Ok(new { isBlockedByMe, hasBlockedMe, isBlocked = isBlockedByMe || hasBlockedMe });
    }

    [HttpPost("{userId:int}")]
    public async Task<IActionResult> Block(int userId)
    {
        var (success, error) = await _block.BlockUserAsync(GetUserId(), userId);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpDelete("{userId:int}")]
    public async Task<IActionResult> Unblock(int userId)
    {
        var (success, error) = await _block.UnblockUserAsync(GetUserId(), userId);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
