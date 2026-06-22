using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs;
using PingMe.Models;

namespace PingMe.Services;

public interface ISavedMessageService
{
    Task<(bool Success, int StatusCode, string? Error)> SaveAsync(int currentUserId, int messageId);
    Task<(bool Success, int StatusCode, string? Error)> UnsaveAsync(int currentUserId, int messageId);
    Task<List<SavedMessageDto>> GetSavedMessagesAsync(int currentUserId, SavedMessageFilterDto filter);
    Task<Dictionary<int, bool>> CheckSavedAsync(int currentUserId, List<int> messageIds);
}

public class SavedMessageService : ISavedMessageService
{
    private readonly AppDbContext _db;

    public SavedMessageService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, int StatusCode, string? Error)> SaveAsync(int currentUserId, int messageId)
    {
        var access = await GetMessageAccessAsync(currentUserId, messageId);
        if (!access.Exists)
            return (false, StatusCodes.Status404NotFound, "Tin nhắn không tồn tại.");

        if (!access.CanAccess)
            return (false, StatusCodes.Status403Forbidden, "Bạn không có quyền lưu tin nhắn này.");

        var exists = await _db.SavedMessages.AnyAsync(s =>
            s.UserId == currentUserId &&
            s.MessageId == messageId);

        if (exists)
            return (true, StatusCodes.Status204NoContent, null);

        _db.SavedMessages.Add(new SavedMessage
        {
            UserId = currentUserId,
            MessageId = messageId,
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            foreach (var entry in ex.Entries)
                entry.State = EntityState.Detached;
        }

        return (true, StatusCodes.Status204NoContent, null);
    }

    public async Task<(bool Success, int StatusCode, string? Error)> UnsaveAsync(int currentUserId, int messageId)
    {
        var saved = await _db.SavedMessages.FirstOrDefaultAsync(s =>
            s.UserId == currentUserId &&
            s.MessageId == messageId);

        if (saved is null)
            return (true, StatusCodes.Status204NoContent, null);

        _db.SavedMessages.Remove(saved);
        await _db.SaveChangesAsync();

        return (true, StatusCodes.Status204NoContent, null);
    }

    public async Task<List<SavedMessageDto>> GetSavedMessagesAsync(int currentUserId, SavedMessageFilterDto filter)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var type = filter.Type?.Trim().ToLowerInvariant();

        var query = _db.SavedMessages
            .AsNoTracking()
            .Where(s => s.UserId == currentUserId)
            .Where(s => !s.Message.IsDeleted)
            .Where(s =>
                (s.Message.GroupId.HasValue &&
                 _db.GroupMembers.Any(gm =>
                     gm.GroupId == s.Message.GroupId.Value &&
                     gm.UserId == currentUserId &&
                     !gm.Group.IsDeleted)) ||
                (!s.Message.GroupId.HasValue &&
                 (s.Message.SenderId == currentUserId || s.Message.ReceiverId == currentUserId)));

        if (type == "dm")
            query = query.Where(s => !s.Message.GroupId.HasValue);
        else if (type == "group")
            query = query.Where(s => s.Message.GroupId.HasValue);

        if (filter.GroupId.HasValue)
            query = query.Where(s => s.Message.GroupId == filter.GroupId.Value);

        if (filter.PeerUserId.HasValue)
        {
            var peerId = filter.PeerUserId.Value;
            query = query.Where(s =>
                !s.Message.GroupId.HasValue &&
                ((s.Message.SenderId == currentUserId && s.Message.ReceiverId == peerId) ||
                 (s.Message.SenderId == peerId && s.Message.ReceiverId == currentUserId)));
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SavedMessageDto
            {
                Id = s.Id,
                MessageId = s.MessageId,
                SenderId = s.Message.SenderId,
                SenderName = s.Message.Sender.DisplayName,
                SenderAvatarUrl = s.Message.Sender.AvatarUrl,
                Content = s.Message.Content,
                MessageType = s.Message.MessageType.ToString(),
                FileUrl = s.Message.Attachments
                    .OrderBy(a => a.Id)
                    .Select(a => a.FileUrl)
                    .FirstOrDefault(),
                FileName = s.Message.Attachments
                    .OrderBy(a => a.Id)
                    .Select(a => a.FileName)
                    .FirstOrDefault(),
                CreatedAt = s.Message.CreatedAt,
                SavedAt = s.CreatedAt,
                GroupId = s.Message.GroupId,
                GroupName = s.Message.Group == null ? null : s.Message.Group.Name,
                PeerUserId = s.Message.GroupId.HasValue
                    ? null
                    : s.Message.SenderId == currentUserId
                        ? s.Message.ReceiverId
                        : s.Message.SenderId,
                PeerDisplayName = s.Message.GroupId.HasValue
                    ? null
                    : s.Message.SenderId == currentUserId
                        ? s.Message.Receiver == null ? null : s.Message.Receiver.DisplayName
                        : s.Message.Sender.DisplayName,
                ConversationType = s.Message.GroupId.HasValue ? "Group" : "DM"
            })
            .ToListAsync();
    }

    public async Task<Dictionary<int, bool>> CheckSavedAsync(int currentUserId, List<int> messageIds)
    {
        var ids = messageIds
            .Where(id => id > 0)
            .Distinct()
            .Take(200)
            .ToList();

        if (ids.Count == 0)
            return [];

        var savedIds = await _db.SavedMessages
            .AsNoTracking()
            .Where(s => s.UserId == currentUserId && ids.Contains(s.MessageId))
            .Select(s => s.MessageId)
            .ToListAsync();

        var savedSet = savedIds.ToHashSet();
        return ids.ToDictionary(id => id, id => savedSet.Contains(id));
    }

    private async Task<(bool Exists, bool CanAccess)> GetMessageAccessAsync(int currentUserId, int messageId)
    {
        var message = await _db.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

        if (message is null)
            return (false, false);

        if (message.GroupId.HasValue)
        {
            var isMember = await _db.GroupMembers.AnyAsync(gm =>
                gm.GroupId == message.GroupId.Value &&
                gm.UserId == currentUserId &&
                !gm.Group.IsDeleted);

            return (true, isMember);
        }

        return (true, message.SenderId == currentUserId || message.ReceiverId == currentUserId);
    }
}
