using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using PingMe.Data;
using PingMe.DTOs.Conversation;
using PingMe.DTOs.Message;
using PingMe.Hubs;
using PingMe.Models;
using System.Text.Json;

namespace PingMe.Services;

public interface IConversationService
{
    Task<List<ConversationResponse>> GetConversationsAsync(int userId);
    Task<(bool Success, string? Error)> PinConversationAsync(int userId, int? peerId, int? groupId);
    Task<(bool Success, string? Error)> UnpinConversationAsync(int userId, int? peerId, int? groupId);
    Task SetNicknameAsync(int userId, SetNicknameRequest request);
    Task SetBackgroundAsync(int userId, SetBackgroundRequest request);
    Task<(bool Success, string? Error)> ClearDmHistoryAsync(int userId, int peerId);
    Task<(bool Success, string? Error)> ClearGroupHistoryAsync(int userId, int groupId);
}

public class ConversationService : IConversationService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ChatHub> _hub;
    public ConversationService(AppDbContext db, IHubContext<ChatHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<List<ConversationResponse>> GetConversationsAsync(int userId)
    {
        var result = new List<ConversationResponse>();

        // 1. Lấy danh sách bạn bè (DM)
        var dmPeerIds = await _db.Messages
            .Where(m => (m.SenderId == userId || m.ReceiverId == userId) && m.GroupId == null && !m.IsDeleted)
            .Select(m => m.SenderId == userId ? m.ReceiverId!.Value : m.SenderId)
            .Distinct()
            .ToListAsync();

        if (dmPeerIds.Any())
        {
            // Lấy tất cả thông tin user một lần
            var peers = await _db.Users
                .Where(u => dmPeerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var nicknames = await _db.ConversationNicknames
                .Where(n => n.SetByUserId == userId && n.GroupId == null && dmPeerIds.Contains(n.TargetUserId))
                .ToDictionaryAsync(n => n.TargetUserId, n => n.Nickname);

            // Lấy last message cho tất cả peer (batch)
            var lastMessages = await _db.Messages
                .Where(m =>
                    m.GroupId == null &&
                    m.ReceiverId != null &&
                    !m.IsDeleted &&
                    ((m.SenderId == userId && dmPeerIds.Contains(m.ReceiverId.Value)) ||
                     (m.ReceiverId == userId && dmPeerIds.Contains(m.SenderId))))
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId!.Value : m.SenderId)
                .Select(g => new { PeerId = g.Key, LastMsg = g.OrderByDescending(x => x.CreatedAt).FirstOrDefault() })
                .ToDictionaryAsync(x => x.PeerId, x => x.LastMsg);

            // Unread count cho mỗi peer (batch)
            var unreadCounts = await _db.Messages
                .Where(m => m.GroupId == null && m.ReceiverId == userId && !m.IsDeleted && dmPeerIds.Contains(m.SenderId) &&
                    !_db.MessageReadReceipts.Any(r => r.MessageId == m.Id && r.UserId == userId))
                .GroupBy(m => m.SenderId)
                .Select(g => new { PeerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PeerId, x => x.Count);

            // Pinned DM
            var pinnedDmIds = await _db.PinnedConversations
                .Where(p => p.UserId == userId && p.PeerUserId != null)
                .Select(p => p.PeerUserId!.Value)
                .ToListAsync();
            var pinnedSet = new HashSet<int>(pinnedDmIds);

            foreach (var peerId in dmPeerIds)
            {
                if (!peers.TryGetValue(peerId, out var peer)) continue;
                lastMessages.TryGetValue(peerId, out var lastMsg);
                unreadCounts.TryGetValue(peerId, out var unread);

                result.Add(new ConversationResponse
                {
                    Type = "dm",
                    PeerId = peerId,
                    PeerDisplayName = nicknames.TryGetValue(peerId, out var nickname)
                        ? nickname
                        : peer.DisplayName,
                    PeerAvatarUrl = peer.AvatarUrl,
                    PeerIsOnline = peer.IsOnline,
                    LastMessage = BuildLastMessagePreview(lastMsg),
                    LastMessageAt = lastMsg?.CreatedAt,
                    UnreadCount = unread,
                    IsPinned = pinnedSet.Contains(peerId)
                });
            }
        }

        // 2. Lấy danh sách nhóm
        var groupIds = await _db.GroupMembers.Where(gm => gm.UserId == userId).Select(gm => gm.GroupId).ToListAsync();
        if (groupIds.Any())
        {
            var groups = await _db.Groups
                .Include(g => g.Members)
                .Where(g => groupIds.Contains(g.Id) && !g.IsDeleted)
                .ToListAsync();

            // Last message group
            var lastGroupMessages = await _db.Messages
                .Where(m => m.GroupId != null && groupIds.Contains(m.GroupId.Value) && !m.IsDeleted)
                .GroupBy(m => m.GroupId!.Value)
                .Select(g => new { GroupId = g.Key, LastMsg = g.OrderByDescending(x => x.CreatedAt).FirstOrDefault() })
                .ToDictionaryAsync(x => x.GroupId, x => x.LastMsg);

            // Unread group
            var groupUnread = await _db.Messages
      .Where(m =>
          m.GroupId != null &&
          groupIds.Contains(m.GroupId.Value) &&
          !m.IsDeleted &&
          m.SenderId != userId &&
          !_db.MessageReadReceipts.Any(r => r.MessageId == m.Id && r.UserId == userId))
      .GroupBy(m => m.GroupId!.Value)
      .Select(g => new { GroupId = g.Key, Count = g.Count() })
      .ToDictionaryAsync(x => x.GroupId, x => x.Count);

            // Pinned group
            var pinnedGroupIds = await _db.PinnedConversations
                .Where(p => p.UserId == userId && p.GroupId != null)
                .Select(p => p.GroupId!.Value)
                .ToListAsync();
            var pinnedGroupSet = new HashSet<int>(pinnedGroupIds);

            foreach (var grp in groups)
            {
                lastGroupMessages.TryGetValue(grp.Id, out var lastMsg);
                groupUnread.TryGetValue(grp.Id, out var unread);
                result.Add(new ConversationResponse
                {
                    Type = "group",
                    GroupId = grp.Id,
                    GroupName = grp.Name,
                    GroupAvatarUrl = grp.AvatarUrl,
                    MemberCount = grp.Members.Count,
                    LastMessage = BuildLastMessagePreview(lastMsg),
                    LastMessageAt = lastMsg?.CreatedAt,
                    UnreadCount = unread,
                    IsPinned = pinnedGroupSet.Contains(grp.Id)
                });
            }
        }

        return result.OrderByDescending(c => c.IsPinned).ThenByDescending(c => c.LastMessageAt).ToList();
    }

    public async Task<(bool Success, string? Error)> PinConversationAsync(int userId, int? peerId, int? groupId)
    {
        var exists = await _db.PinnedConversations.AnyAsync(p =>
            p.UserId == userId && p.PeerUserId == peerId && p.GroupId == groupId);
        if (exists) return (false, "Đã ghim rồi.");

        _db.PinnedConversations.Add(new PinnedConversation
        {
            UserId = userId,
            PeerUserId = peerId,
            GroupId = groupId
        });
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UnpinConversationAsync(int userId, int? peerId, int? groupId)
    {
        var pin = await _db.PinnedConversations.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.PeerUserId == peerId && p.GroupId == groupId);
        if (pin is null) return (false, "Chưa ghim.");

        _db.PinnedConversations.Remove(pin);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task SetNicknameAsync(int userId, SetNicknameRequest request)
    {
        if (request.TargetUserId <= 0)
            return;

        var nickname = request.Nickname?.Trim() ?? string.Empty;

        var existing = await _db.ConversationNicknames.FirstOrDefaultAsync(n =>
            n.SetByUserId == userId && n.TargetUserId == request.TargetUserId && n.GroupId == request.GroupId);

        if (string.IsNullOrWhiteSpace(nickname))
        {
            if (existing is not null)
            {
                _db.ConversationNicknames.Remove(existing);
                await _db.SaveChangesAsync();
            }

            return;
        }

        if (existing is null)
        {
            _db.ConversationNicknames.Add(new ConversationNickname
            {
                SetByUserId = userId,
                TargetUserId = request.TargetUserId,
                GroupId = request.GroupId,
                Nickname = nickname
            });
        }
        else
        {
            existing.Nickname = nickname;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();

        if (request.GroupId.HasValue)
            await AddGroupSystemMessageAsync(request.GroupId.Value, userId, $"{await GetDisplayNameAsync(userId)} đã đổi biệt danh");
        else
            await AddDmSystemMessageAsync(userId, request.TargetUserId, $"{await GetDisplayNameAsync(userId)} đã đổi biệt danh");
    }

    public async Task SetBackgroundAsync(int userId, SetBackgroundRequest request)
    {
        var existing = await _db.ConversationBackgrounds.FirstOrDefaultAsync(b =>
            b.UserId == userId && b.PeerUserId == request.PeerUserId && b.GroupId == request.GroupId);

        var bgType = Enum.Parse<BackgroundType>(request.BackgroundType, true);

        if (existing is null)
        {
            _db.ConversationBackgrounds.Add(new ConversationBackground
            {
                UserId = userId,
                PeerUserId = request.PeerUserId,
                GroupId = request.GroupId,
                BackgroundType = bgType,
                BackgroundValue = request.BackgroundValue
            });
        }
        else
        {
            existing.BackgroundType = bgType;
            existing.BackgroundValue = request.BackgroundValue;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();

        if (request.GroupId.HasValue)
            await AddGroupSystemMessageAsync(request.GroupId.Value, userId, $"{await GetDisplayNameAsync(userId)} đã đổi background nhóm");
        else if (request.PeerUserId.HasValue)
            await AddDmSystemMessageAsync(userId, request.PeerUserId.Value, $"{await GetDisplayNameAsync(userId)} đã đổi background");
    }

    public async Task<(bool Success, string? Error)> ClearDmHistoryAsync(int userId, int peerId)
    {
        var messages = await _db.Messages
            .Where(m => m.GroupId == null && 
                        ((m.SenderId == userId && m.ReceiverId == peerId) || 
                         (m.SenderId == peerId && m.ReceiverId == userId)))
            .ToListAsync();
            
        _db.Messages.RemoveRange(messages);
        
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ClearGroupHistoryAsync(int userId, int groupId)
    {
        var member = await _db.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        if (member == null) return (false, "Bạn không phải là thành viên nhóm này.");
        
        member.ClearedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    private async Task<string> GetDisplayNameAsync(int userId)
    {
        return await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync() ?? "Người dùng";
    }

    private async Task AddDmSystemMessageAsync(int senderId, int receiverId, string content)
    {
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            MessageType = MessageType.System,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var dto = await BuildSystemMessageResponseAsync(message.Id);
        if (dto is null)
            return;

        await _hub.Clients.Group($"user_{senderId}").SendAsync("ReceiveMessage", dto);
        await _hub.Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", dto);
    }

    private async Task AddGroupSystemMessageAsync(int groupId, int senderId, string content)
    {
        var message = new Message
        {
            SenderId = senderId,
            GroupId = groupId,
            Content = content,
            MessageType = MessageType.System,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var dto = await BuildSystemMessageResponseAsync(message.Id);
        if (dto is not null)
            await _hub.Clients.Group($"group_{groupId}").SendAsync("ReceiveMessage", dto);
    }

    private async Task<MessageResponse?> BuildSystemMessageResponseAsync(int messageId)
    {
        var message = await _db.Messages
            .AsNoTracking()
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message is null)
            return null;

        return new MessageResponse
        {
            Id = message.Id,
            SenderId = message.SenderId,
            SenderDisplayName = message.Sender.DisplayName,
            SenderAvatarUrl = message.Sender.AvatarUrl,
            GroupId = message.GroupId,
            ReceiverId = message.ReceiverId,
            Content = message.Content,
            MessageType = message.MessageType.ToString(),
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt
        };
    }

    private static string? BuildLastMessagePreview(Message? message)
    {
        if (message is null) return null;
        if (message.IsDeleted) return "Tin nhắn đã thu hồi";

        var preview = message.MessageType switch
        {
            MessageType.Image => "🖼️ Đã gửi một ảnh",
            MessageType.File => $"📎 {message.Content ?? "Đã gửi một file"}",
            MessageType.Reminder => BuildCommandPreview(message.Content, "reminder") ?? "⏰ Nhắc việc",
            MessageType.Task => BuildCommandPreview(message.Content, "task") ?? "✅ Task",
            MessageType.Vulnerability => BuildCommandPreview(message.Content, "vulnerability") ?? "🐞 Finding",
            MessageType.Cve => BuildCommandPreview(message.Content, "ioc") ?? "🛡 IOC",
            _ => message.Content
        };

        preview = BuildCommandPreview(preview, null) ?? preview;
        return TruncatePreview(preview);
    }

    private static string? BuildCommandPreview(string? content, string? expectedType)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var trimmed = content.Trim();

        if (trimmed.StartsWith("/ioc ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = trimmed.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
                return $"🛡 IOC: {parts[2]}";
        }

        if (!trimmed.StartsWith('{'))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;
            var type = GetString(root, "type");

            if (!string.IsNullOrWhiteSpace(expectedType) &&
                !string.Equals(type, expectedType, StringComparison.OrdinalIgnoreCase) &&
                !(string.Equals(expectedType, "vulnerability", StringComparison.OrdinalIgnoreCase) &&
                  string.Equals(type, "finding", StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            return type?.Trim().ToLowerInvariant() switch
            {
                "reminder" => $"⏰ Nhắc việc: {GetString(root, "text") ?? "nhắc việc"}",
                "task" => $"✅ Task: {GetString(root, "title") ?? "task"}",
                "vulnerability" or "finding" => $"🐞 Finding: {GetString(root, "title") ?? "finding"}",
                "ioc" => $"🛡 IOC: {GetString(root, "value") ?? GetString(root, "title") ?? "IOC"}",
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? TruncatePreview(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var normalized = value.Replace("\r", " ").Replace("\n", " ").Trim();
        return normalized.Length <= 140 ? normalized : normalized[..137] + "...";
    }
}
