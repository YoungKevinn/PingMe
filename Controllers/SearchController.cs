using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs.Search;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly ISearchService _search;

    public SearchController(ISearchService search)
    {
        _search = search;
    }

    [HttpGet("global")]
    public async Task<IActionResult> GlobalSearch([FromQuery] GlobalSearchRequestDto request)
    {
        var result = await _search.GlobalSearchAsync(GetUserId(), request);
        return Ok(result);
    }

    // GET /api/search?q=abc
    // GET /api/search?q=
    // Nếu q rỗng: trả về danh bạ user, messages để rỗng.
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? q = "", [FromQuery] int limit = 50)
    {
        var result = await _search.SearchAsync(GetUserId(), q ?? string.Empty, limit);
        return Ok(result);
    }

    // GET /api/search/messages?q=abc&peerId=2
    // GET /api/search/messages?q=abc&groupId=1
    // Dùng cho tìm trong đúng đoạn chat hiện tại rồi nhảy tới highlight.
    [HttpGet("messages")]
    public async Task<IActionResult> SearchMessagesInConversation(
        [FromQuery] string q,
        [FromQuery] int? peerId,
        [FromQuery] int? groupId,
        [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Query không được rỗng." });

        if (!peerId.HasValue && !groupId.HasValue)
            return BadRequest(new { message = "Cần peerId hoặc groupId." });

        var results = await _search.SearchMessagesInConversationAsync(
            GetUserId(),
            q,
            peerId,
            groupId,
            limit);

        return Ok(results);
    }

    private int GetUserId()
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(rawId))
            throw new UnauthorizedAccessException("Không tìm thấy user id trong token.");

        return int.Parse(rawId);
    }
}
