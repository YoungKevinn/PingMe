using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PingMe.DTOs.Message;
using PingMe.Services;
using System.Security.Claims;

namespace PingMe.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
public class MessageController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessageController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
    {
        var (success, error, message) = await _messageService.SendMessageAsync(GetUserId(), request);
        if (!success)
        {
            if (string.Equals(error, "Bạn không còn là thành viên của nhóm này.", StringComparison.Ordinal))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = error });

            return BadRequest(new { message = error });
        }
        return Ok(message);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] int? receiverId,
        [FromForm] int? groupId,
        [FromForm] int? replyToMessageId,
        [FromForm] DateTime? expiresAt)
    {
        var backendBaseUrl = $"{Request.Scheme}://{Request.Host}";

        var (success, error, message) = await _messageService.UploadMessageAsync(
            GetUserId(),
            receiverId,
            groupId,
            replyToMessageId,
            file,
            backendBaseUrl,
            expiresAt);

        if (!success)
        {
            if (string.Equals(error, "Bạn không còn là thành viên của nhóm này.", StringComparison.Ordinal))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = error });

            return BadRequest(new { message = error });
        }
        return Ok(message);
    }

    [HttpGet("dm/{peerId:int}")]
    public async Task<IActionResult> GetDmMessages(int peerId, [FromQuery] int? before, [FromQuery] int limit = 30)
    {
        var messages = await _messageService.GetDmMessagesAsync(GetUserId(), peerId, before, Math.Min(limit, 50));
        return Ok(messages);
    }

    [HttpGet("group/{groupId:int}")]
    public async Task<IActionResult> GetGroupMessages(int groupId, [FromQuery] int? before, [FromQuery] int limit = 30)
    {
        try
        {
            var messages = await _messageService.GetGroupMessagesAsync(GetUserId(), groupId, before, Math.Min(limit, 50));
            return Ok(messages);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpGet("{messageId:int}/context")]
    public async Task<IActionResult> GetMessageContext(
        int messageId,
        [FromQuery] int takeBefore = 20,
        [FromQuery] int takeAfter = 20)
    {
        var messages = await _messageService.GetMessageContextAsync(GetUserId(), messageId, takeBefore, takeAfter);
        if (messages.Count == 0) return NotFound(new { message = "Không tìm thấy tin nhắn hoặc bạn không có quyền xem." });
        return Ok(messages);
    }

    [HttpGet("pinned")]
    public async Task<IActionResult> GetPinnedMessages([FromQuery] int? groupId, [FromQuery] int? peerId)
    {
        var messages = await _messageService.GetPinnedMessagesAsync(groupId, peerId, GetUserId());
        return Ok(messages);
    }

    [HttpPut("{messageId:int}")]
    public async Task<IActionResult> Edit(int messageId, [FromBody] EditMessageRequest request)
    {
        var (success, error) = await _messageService.EditMessageAsync(messageId, GetUserId(), request.Content);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpGet("attachments")]
    public async Task<IActionResult> GetConversationAttachments(
        [FromQuery] int? groupId,
        [FromQuery] int? peerId,
        [FromQuery] string type = "all",
        [FromQuery] int limit = 80)
    {
        var attachments = await _messageService.GetConversationAttachmentsAsync(
            GetUserId(),
            groupId,
            peerId,
            type,
            limit);
        return Ok(attachments);
    }

    [HttpDelete("{messageId:int}")]
    public async Task<IActionResult> Delete(int messageId)
    {
        var (success, error) = await _messageService.DeleteMessageAsync(messageId, GetUserId());
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpPatch("{messageId:int}/pin")]
    public async Task<IActionResult> Pin(int messageId, [FromBody] PinMessageRequest request)
    {
        var (success, error) = await _messageService.PinMessageAsync(messageId, GetUserId(), request.IsPinned);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpPost("{messageId:int}/forward")]
    public async Task<IActionResult> Forward(int messageId, [FromQuery] int? receiverId, [FromQuery] int? groupId)
    {
        var (success, error, message) = await _messageService.ForwardMessageAsync(messageId, GetUserId(), receiverId, groupId);
        if (!success) return BadRequest(new { message = error });
        return Ok(message);
    }

    [HttpGet("{messageId}/history")]
    public async Task<IActionResult> GetEditHistory(int messageId)
    {
        var history = await _messageService.GetEditHistoryAsync(messageId, GetUserId());
        return Ok(history);
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
}
