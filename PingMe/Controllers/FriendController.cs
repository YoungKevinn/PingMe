using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs.Friend;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/friends")]
[Authorize]
public class FriendController : ControllerBase
{
    private readonly IFriendService _friend;
    public FriendController(IFriendService friend) => _friend = friend;

    // GET /api/friends  → danh sách bạn bè
    [HttpGet]
    public async Task<IActionResult> GetFriends()
    {
        var userId = GetUserId();
        var list = await _friend.GetFriendsAsync(userId);
        return Ok(list);
    }

    // POST /api/friends/request  → gửi lời mời kết bạn
    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDto dto)
    {
        if (dto == null || dto.TargetUserId <= 0)
            return BadRequest(new { message = "Thiếu hoặc sai TargetUserId." });

        try
        {
            var userId = GetUserId();
            var (success, error) = await _friend.SendRequestAsync(userId, dto.TargetUserId);
            if (!success) return BadRequest(new { message = error });
            return NoContent();
        }
        catch (Exception ex)
        {
            // tránh trả 500 trần cho client
            return StatusCode(500, new { message = $"Lỗi server: {ex.Message}" });
        }
    }

    // POST /api/friends/accept/{requestId}  → chấp nhận lời mời
    [HttpPost("accept/{requestId:int}")]
    public async Task<IActionResult> Accept(int requestId)
    {
        var userId = GetUserId();
        var (success, error) = await _friend.RespondRequestAsync(requestId, userId, true);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    // POST /api/friends/decline/{requestId}  → từ chối lời mời
    [HttpPost("decline/{requestId:int}")]
    public async Task<IActionResult> Decline(int requestId)
    {
        var userId = GetUserId();
        var (success, error) = await _friend.RespondRequestAsync(requestId, userId, false);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    // POST /api/friends/request/{id}/respond?accept=true|false (giữ tương thích endpoint cũ)
    [HttpPost("request/{requestId:int}/respond")]
    public async Task<IActionResult> Respond(int requestId, [FromQuery] bool accept)
    {
        var userId = GetUserId();
        var (success, error) = await _friend.RespondRequestAsync(requestId, userId, accept);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    // DELETE /api/friends/cancel/{requestId}  → hủy lời mời đã gửi
    [HttpDelete("cancel/{requestId:int}")]
    public async Task<IActionResult> Cancel(int requestId)
    {
        var userId = GetUserId();
        var (success, error) = await _friend.CancelRequestAsync(requestId, userId);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    // DELETE /api/friends/{friendId}  → hủy kết bạn
    [HttpDelete("{friendId:int}")]
    public async Task<IActionResult> Unfriend(int friendId)
    {
        var userId = GetUserId();
        var (success, error) = await _friend.UnfriendAsync(userId, friendId);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    // GET /api/friends/requests  → lời mời nhận được
    [HttpGet("requests")]
    public async Task<IActionResult> Requests()
    {
        var userId = GetUserId();
        var list = await _friend.GetIncomingRequestsAsync(userId);
        return Ok(list);
    }

    // GET /api/friends/sent  → lời mời đã gửi
    [HttpGet("sent")]
    public async Task<IActionResult> Sent()
    {
        var userId = GetUserId();
        var list = await _friend.GetOutgoingRequestsAsync(userId);
        return Ok(list);
    }

    // GET /api/friends/requests/incoming  (giữ tương thích endpoint cũ)
    [HttpGet("requests/incoming")]
    public async Task<IActionResult> Incoming() => await Requests();

    // GET /api/friends/requests/outgoing  (giữ tương thích endpoint cũ)
    [HttpGet("requests/outgoing")]
    public async Task<IActionResult> Outgoing() => await Sent();

    // GET /api/friends/check/{otherId}  → kiểm tra đã là bạn chưa
    [HttpGet("check/{otherId:int}")]
    public async Task<IActionResult> Check(int otherId)
    {
        var userId = GetUserId();
        var areFriends = await _friend.AreFriendsAsync(userId, otherId);
        return Ok(new { areFriends });
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
