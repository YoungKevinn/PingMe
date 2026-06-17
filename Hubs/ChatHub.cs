using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.Services;

namespace PingMe.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    private readonly ISignalRConnectionTracker _connectionTracker;

    public ChatHub(AppDbContext db, ISignalRConnectionTracker connectionTracker)
    {
        _db = db;
        _connectionTracker = connectionTracker;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId is null) return;

        _connectionTracker.AddConnection(userId.Value, Context.ConnectionId);

        // Join user's personal room
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");

        // Join all group rooms
        var groupIds = await _db.GroupMembers
            .Where(gm => gm.UserId == userId.Value)
            .Select(gm => gm.GroupId)
            .ToListAsync();

        foreach (var gid in groupIds)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{gid}");

        // Update online status
        var user = await _db.Users.FindAsync(userId.Value);
        if (user is not null)
        {
            user.IsOnline  = true;
            user.LastSeen  = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        await Clients.Others.SendAsync("UserStatusChanged", new { userId, isOnline = true });
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId is null) return;

        _connectionTracker.RemoveConnection(userId.Value, Context.ConnectionId);

        var user = await _db.Users.FindAsync(userId.Value);
        if (user is not null)
        {
            user.IsOnline  = false;
            user.LastSeen  = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        await Clients.Others.SendAsync("UserStatusChanged", new { userId, isOnline = false, lastSeen = DateTime.UtcNow });
        await base.OnDisconnectedAsync(exception);
    }

    // ─── Client → Server events ────────────────────────────────────────────

    public async Task JoinRoom(string roomName)
    {
        var userId = GetUserId();
        if (userId is null || string.IsNullOrWhiteSpace(roomName))
            return;

        if (roomName == $"user_{userId.Value}")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
            return;
        }

        if (roomName.StartsWith("group_", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(roomName["group_".Length..], out var groupId))
        {
            var isMember = await _db.GroupMembers
                .AsNoTracking()
                .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId.Value && !gm.Group.IsDeleted);

            if (isMember)
                await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }
    }

    public async Task LeaveRoom(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
    }

    public async Task StartTyping(int? groupId, int? receiverId)
    {
        var userId = GetUserId();
        if (userId is null) return;

        var user = await _db.Users.FindAsync(userId.Value);
        var payload = new { userId, displayName = user?.DisplayName, groupId, receiverId };

        if (groupId.HasValue)
            await Clients.OthersInGroup($"group_{groupId}").SendAsync("UserTyping", payload);
        else if (receiverId.HasValue)
            await Clients.Group($"user_{receiverId}").SendAsync("UserTyping", payload);
    }

    public async Task StopTyping(int? groupId, int? receiverId)
    {
        var userId = GetUserId();
        if (userId is null) return;

        var payload = new { userId, groupId, receiverId, isTyping = false };

        if (groupId.HasValue)
            await Clients.OthersInGroup($"group_{groupId}").SendAsync("UserTyping", payload);
        else if (receiverId.HasValue)
            await Clients.Group($"user_{receiverId}").SendAsync("UserTyping", payload);
    }

    public async Task MarkAsRead(int messageId)
    {
        var userId = GetUserId();
        if (userId is null) return;

        var exists = await _db.MessageReadReceipts.AnyAsync(r => r.MessageId == messageId && r.UserId == userId.Value);
        if (!exists)
        {
            _db.MessageReadReceipts.Add(new Models.MessageReadReceipt
            {
                MessageId = messageId,
                UserId    = userId.Value
            });
            await _db.SaveChangesAsync();

            var message = await _db.Messages.FindAsync(messageId);
            if (message?.GroupId.HasValue == true)
                await Clients.Group($"group_{message.GroupId}").SendAsync("MessageRead", new { messageId, userId });
            else if (message?.SenderId is not null)
                await Clients.Group($"user_{message.SenderId}").SendAsync("MessageRead", new { messageId, userId });
        }
    }

    // ─── WebRTC Call Events ──────────────────────────────────────────────────

    public async Task CallUser(int receiverId, bool isVideoCall)
    {
        var userId = GetUserId();
        if (userId is null) return;

        var user = await _db.Users.FindAsync(userId.Value);
        var payload = new { callerId = userId.Value, callerName = user?.DisplayName, callerAvatar = user?.AvatarUrl, isVideoCall };

        await Clients.Group($"user_{receiverId}").SendAsync("IncomingCall", payload);
    }

    public async Task AnswerCall(int callerId, bool accepted)
    {
        var userId = GetUserId();
        if (userId is null) return;

        await Clients.Group($"user_{callerId}").SendAsync("CallAnswered", new { responderId = userId.Value, accepted });
    }

    public async Task SendWebRTCSignal(int targetId, string type, string payload)
    {
        var userId = GetUserId();
        if (userId is null) return;

        await Clients.Group($"user_{targetId}").SendAsync("ReceiveWebRTCSignal", new { senderId = userId.Value, type, payload });
    }

    public async Task EndCall(int targetId, int durationSeconds, bool isVideoCall, string reason)
    {
        var userId = GetUserId();
        if (userId is null) return;

        string content = reason switch
        {
            "Missed" => isVideoCall ? "Missed video call" : "Missed audio call",
            "Rejected" => isVideoCall ? "Video call declined" : "Audio call declined",
            _ => (isVideoCall ? "Video call ended" : "Audio call ended") + (durationSeconds > 0 ? $" ({TimeSpan.FromSeconds(durationSeconds):mm\\:ss})" : "")
        };

        var message = new Models.Message
        {
            SenderId = userId.Value,
            ReceiverId = targetId,
            Content = content,
            MessageType = Models.MessageType.Call,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var sender = await _db.Users.FindAsync(userId.Value);

        await Clients.Group($"user_{targetId}").SendAsync("CallEnded", new { senderId = userId.Value, reason });

        var responseMsg = new
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderDisplayName = sender?.DisplayName ?? "",
            SenderAvatarUrl = sender?.AvatarUrl,
            ReceiverId = message.ReceiverId,
            Content = message.Content,
            MessageType = message.MessageType.ToString(),
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt
        };

        await Clients.Group($"user_{targetId}").SendAsync("ReceiveMessage", responseMsg);
        await Clients.Group($"user_{userId.Value}").SendAsync("ReceiveMessage", responseMsg);
    }

    // ─── Helper ────────────────────────────────────────────────────────────

    private int? GetUserId()
    {
        var claim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                    ?? Context.User?.FindFirst("sub");
        return claim is null ? null : int.Parse(claim.Value);
    }
}
