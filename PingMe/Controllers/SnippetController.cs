using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs.Snippet;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/snippets")]
[Authorize]
public class SnippetController : ControllerBase
{
    private readonly ISnippetService _snippet;

    public SnippetController(ISnippetService snippet)
    {
        _snippet = snippet;
    }

    // POST /api/snippets
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSnippetRequest request)
    {
        var snippet = await _snippet.CreateSnippetAsync(GetUserId(), request);
        return Ok(snippet);
    }

    // PUT /api/snippets/{id}
    [HttpPut("{snippetId:int}")]
    public async Task<IActionResult> Update(int snippetId, [FromBody] UpdateSnippetRequest request)
    {
        var result = await _snippet.UpdateSnippetAsync(snippetId, GetUserId(), request);

        if (result.Snippet is null)
            return BadRequest(new { message = result.Error });

        return Ok(result.Snippet);
    }

    // GET /api/snippets
    [HttpGet]
    public async Task<IActionResult> GetMine()
    {
        var snippets = await _snippet.GetUserSnippetsAsync(GetUserId());
        return Ok(snippets);
    }

    // GET /api/snippets/search?title=&language=&content=
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] SnippetSearchRequest request)
    {
        var snippets = await _snippet.SearchSnippetsAsync(GetUserId(), request);
        return Ok(snippets);
    }

    // POST /api/snippets/{id}/revoke
    [HttpPost("{snippetId:int}/revoke")]
    public async Task<IActionResult> Revoke(int snippetId)
    {
        var result = await _snippet.RevokeSnippetAsync(snippetId, GetUserId());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Link chia sẻ đã bị thu hồi." });
    }

    // POST /api/snippets/{id}/restore
    [HttpPost("{snippetId:int}/restore")]
    public async Task<IActionResult> Restore(int snippetId)
    {
        var result = await _snippet.RestoreSnippetAsync(snippetId, GetUserId());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Link chia sẻ đã được khôi phục." });
    }

    // DELETE /api/snippets/{id}
    [HttpDelete("{snippetId:int}")]
    public async Task<IActionResult> Delete(int snippetId)
    {
        var result = await _snippet.DeleteSnippetAsync(snippetId, GetUserId());

        if (!result.Success)
            return BadRequest(new { message = result.Error });

        return NoContent();
    }

    // GET /api/snippets/share/{token}
    [AllowAnonymous]
    [HttpGet("share/{token}")]
    public async Task<IActionResult> GetByToken(string token)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var currentUserId = TryGetUserId();

        var snippet = await _snippet.GetByTokenAsync(token, currentUserId, ip, ua);

        if (snippet is null)
            return NotFound(new { message = "Snippet không tồn tại, đã hết hạn hoặc đã bị thu hồi." });

        return Ok(snippet);
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
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

        return int.TryParse(value, out var id) ? id : null;
    }
}