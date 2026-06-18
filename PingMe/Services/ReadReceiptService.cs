using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.Hubs;
using PingMe.Models;

namespace PingMe.Services;

public interface IReadReceiptService
{
    Task MarkAsReadAsync(int messageId, int userId);
    Task MarkConversationAsReadAsync(int userId, int? peerId, int? groupId);
}

public class ReadReceiptService : IReadReceiptService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ChatHub> _hub;

    public ReadReceiptService(AppDbContext db, IHubContext<ChatHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task MarkAsReadAsync(int messageId, int userId)
    {
        var message = await _db.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

        if (message is null)
            return;

        // Không tự mark read tin nhắn mình gửi
        if (message.SenderId == userId)
            return;

        var exists = await _db.MessageReadReceipts
            .AnyAsync(r => r.MessageId == messageId && r.UserId == userId);

        if (!exists)
        {
            _db.MessageReadReceipts.Add(new MessageReadReceipt
            {
                MessageId = messageId,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        var payload = new
        {
            messageId,
            messageIds = new[] { messageId },
            userId
        };

        if (message.GroupId.HasValue)
        {
            await _hub.Clients.Group($"group_{message.GroupId.Value}")
                .SendAsync("MessageRead", payload);
        }
        else
        {
            await _hub.Clients.Group($"user_{message.SenderId}")
                .SendAsync("MessageRead", payload);

            if (message.ReceiverId.HasValue)
            {
                await _hub.Clients.Group($"user_{message.ReceiverId.Value}")
                    .SendAsync("MessageRead", payload);
            }
        }
    }

    public async Task MarkConversationAsReadAsync(int userId, int? peerId, int? groupId)
    {
        IQueryable<Message> query;

        if (groupId.HasValue)
        {
            var isMember = await _db.GroupMembers
                .AsNoTracking()
                .AnyAsync(gm => gm.GroupId == groupId.Value && gm.UserId == userId);

            if (!isMember)
                return;

            query = _db.Messages
                .Where(m => m.GroupId == groupId.Value && !m.IsDeleted && m.SenderId != userId);
        }
        else if (peerId.HasValue)
        {
            query = _db.Messages
                .Where(m =>
                    !m.IsDeleted &&
                    m.GroupId == null &&
                    m.SenderId == peerId.Value &&
                    m.ReceiverId == userId);
        }
        else
        {
            return;
        }

        var unreadMessageIds = await query
            .Where(m => !_db.MessageReadReceipts.Any(r =>
                r.MessageId == m.Id &&
                r.UserId == userId))
            .Select(m => m.Id)
            .ToListAsync();

        if (!unreadMessageIds.Any())
            return;

        foreach (var id in unreadMessageIds)
        {
            _db.MessageReadReceipts.Add(new MessageReadReceipt
            {
                MessageId = id,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        var payload = new
        {
            messageId = unreadMessageIds.Last(),
            messageIds = unreadMessageIds,
            userId
        };

        if (groupId.HasValue)
        {
            await _hub.Clients.Group($"group_{groupId.Value}")
                .SendAsync("MessageRead", payload);
        }
        else if (peerId.HasValue)
        {
            await _hub.Clients.Group($"user_{userId}")
                .SendAsync("MessageRead", payload);

            await _hub.Clients.Group($"user_{peerId.Value}")
                .SendAsync("MessageRead", payload);
        }
    }
}