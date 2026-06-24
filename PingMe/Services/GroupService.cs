using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs.Group;
using PingMe.DTOs.Message;
using PingMe.Hubs;
using PingMe.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
namespace PingMe.Services;

public interface IGroupService
{
    Task<(bool Success, string? Error, GroupResponse? Group)> CreateGroupAsync(int creatorId, CreateGroupRequest request);
    Task<GroupResponse?> GetGroupAsync(int groupId, int userId);
    Task<List<GroupResponse>> GetUserGroupsAsync(int userId);
    Task<(bool Success, string? Error)> UpdateGroupAsync(int groupId, int userId, UpdateGroupRequest request);
    Task<(bool Success, string? Error)> DeleteGroupAsync(int groupId, int userId);
    Task<(bool Success, string? Error)> AddMemberAsync(int groupId, int requesterId, int targetUserId);
    Task<(bool Success, string? Error)> KickMemberAsync(int groupId, int requesterId, int targetUserId);
    Task<(bool Success, string? Error)> UpdateMemberRoleAsync(int groupId, int requesterId, int targetUserId, string role);
    Task<(bool Success, string? Error)> LeaveGroupAsync(int groupId, int userId);
    Task<(bool Success, string? Error, string? AvatarUrl)> UploadGroupAvatarAsync(int groupId, int userId, IFormFile file, string webRootPath);
}

public class GroupService : IGroupService
{
    private const long MaxGroupAvatarSize = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedAvatarContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };
    private static readonly HashSet<string> AllowedAvatarExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly AppDbContext _db;
    private readonly IHubContext<ChatHub> _hub;
    private readonly ISignalRConnectionTracker _connectionTracker;

    public GroupService(AppDbContext db, IHubContext<ChatHub> hub, ISignalRConnectionTracker connectionTracker)
    {
        _db = db;
        _hub = hub;
        _connectionTracker = connectionTracker;
    }

    public async Task<(bool Success, string? Error, GroupResponse? Group)> CreateGroupAsync(
        int creatorId, CreateGroupRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (false, "Tên nhóm không được để trống.", null);

        var uniqueMemberIds = request.MemberIds
            .Where(id => id != creatorId)
            .Distinct()
            .ToList();

        foreach (var memberId in uniqueMemberIds)
        {
            var areFriends = await AreFriendsAsync(creatorId, memberId);

            if (!areFriends)
                return (false, "Chỉ có thể thêm người đã kết bạn vào nhóm.", null);
        }
        var group = new Group
        {
            Name            = request.Name.Trim(),
            Description     = request.Description?.Trim(),
            CreatedByUserId = creatorId,
            CreatedAt       = DateTime.UtcNow,
            UpdatedAt       = DateTime.UtcNow
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        // Add creator as Admin
        var members = new List<GroupMember>
        {
            new() { GroupId = group.Id, UserId = creatorId, Role = GroupMemberRole.Admin }
        };

         // Add other members
        foreach (var memberId in uniqueMemberIds)
        {
            members.Add(new GroupMember
            {
                GroupId = group.Id,
                UserId = memberId,
                Role = GroupMemberRole.Member
            });
        }

        _db.GroupMembers.AddRange(members);
        await _db.SaveChangesAsync();

        var response = await BuildGroupResponseAsync(group.Id);
        await AddSystemMessageAsync(group.Id, creatorId, $"{await GetDisplayNameAsync(creatorId)} đã tạo nhóm {group.Name}");
        return (true, null, response);
    }

    public async Task<GroupResponse?> GetGroupAsync(int groupId, int userId)
    {
        var isMember = await _db.GroupMembers.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        if (!isMember) return null;
        return await BuildGroupResponseAsync(groupId);
    }

    public async Task<List<GroupResponse>> GetUserGroupsAsync(int userId)
    {
        var groupIds = await _db.GroupMembers
            .Where(gm => gm.UserId == userId)
            .Select(gm => gm.GroupId)
            .ToListAsync();

        var result = new List<GroupResponse>();
        foreach (var gid in groupIds)
        {
            var r = await BuildGroupResponseAsync(gid);
            if (r is not null) result.Add(r);
        }
        return result;
    }

    public async Task<(bool Success, string? Error)> UpdateGroupAsync(int groupId, int userId, UpdateGroupRequest request)
    {
        var group = await _db.Groups.FindAsync(groupId);
        if (group is null || group.IsDeleted) return (false, "Nhóm không tồn tại.");

        var role = await GetRoleAsync(groupId, userId);
        if (role is null) return (false, "Bạn không còn là thành viên của nhóm này.");
        if (role == GroupMemberRole.Member) return (false, "Chỉ admin/co-admin mới được sửa nhóm.");

        var oldName = group.Name;
        if (!string.IsNullOrWhiteSpace(request.Name)) group.Name = request.Name.Trim();
        if (request.Description is not null) group.Description = request.Description.Trim();
        group.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (!string.Equals(oldName, group.Name, StringComparison.Ordinal))
            await AddSystemMessageAsync(groupId, userId, $"{await GetDisplayNameAsync(userId)} đã đổi tên nhóm thành {group.Name}");

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteGroupAsync(int groupId, int userId)
    {
        var group = await _db.Groups.FindAsync(groupId);
        if (group is null || group.IsDeleted) return (false, "Nhóm không tồn tại.");
        if (group.CreatedByUserId != userId) return (false, "Chỉ người tạo nhóm mới được giải tán.");

        group.IsDeleted = true;
        group.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _hub.Clients.Group($"group_{groupId}").SendAsync("GroupDeleted", new { groupId });
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AddMemberAsync(int groupId, int requesterId, int targetUserId)
    {
        var role = await GetRoleAsync(groupId, requesterId);
        if (role is null) return (false, "Bạn không còn là thành viên của nhóm này.");
        if (role == GroupMemberRole.Member) return (false, "Chỉ admin/co-admin mới được thêm thành viên.");

        if (requesterId == targetUserId)
            return (false, "Bạn đã ở trong nhóm.");

        var areFriends = await AreFriendsAsync(requesterId, targetUserId);

        if (!areFriends)
            return (false, "Chỉ có thể thêm người đã kết bạn vào nhóm.");

        var exists = await _db.GroupMembers.AnyAsync(gm =>
            gm.GroupId == groupId &&
            gm.UserId == targetUserId);

        if (exists)
            return (false, "User đã là thành viên.");

        _db.GroupMembers.Add(new GroupMember
        {
            GroupId = groupId,
            UserId = targetUserId,
            Role = GroupMemberRole.Member
        });

        await _db.SaveChangesAsync();

        var groupInfo = await _db.Groups
            .AsNoTracking()
            .Where(g => g.Id == groupId)
            .Select(g => new { g.Name, g.AvatarUrl })
            .FirstOrDefaultAsync();
        var systemText = $"{await GetDisplayNameAsync(requesterId)} đã thêm {await GetDisplayNameAsync(targetUserId)} vào nhóm";
        var payload = new
        {
            groupId,
            userId = targetUserId,
            message = systemText,
            groupName = groupInfo?.Name,
            groupAvatarUrl = groupInfo?.AvatarUrl
        };

        await AddSystemMessageAsync(groupId, requesterId, systemText);

        await _hub.Clients.Group($"group_{groupId}").SendAsync("GroupMemberAdded", payload);
        await _hub.Clients.Group($"user_{targetUserId}").SendAsync("GroupMemberAdded", payload);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> KickMemberAsync(int groupId, int requesterId, int targetUserId)
    {
        var requesterRole = await GetRoleAsync(groupId, requesterId);
        if (requesterRole is null) return (false, "Bạn không còn là thành viên của nhóm này.");
        if (requesterRole == GroupMemberRole.Member) return (false, "Không có quyền kick.");

        var targetMember = await _db.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == targetUserId);
        if (targetMember is null) return (false, "User không trong nhóm.");
        if (targetMember.UserId == requesterId) return (false, "Không thể tự kick chính mình.");
        if (targetMember.Role == GroupMemberRole.Admin) return (false, "Không thể kick admin.");
        if (requesterRole == GroupMemberRole.CoAdmin && targetMember.Role != GroupMemberRole.Member)
            return (false, "Co-admin chỉ có thể kick member.");

        _db.GroupMembers.Remove(targetMember);
        await _db.SaveChangesAsync();
        var actorName = await GetDisplayNameAsync(requesterId);
        var targetName = await GetDisplayNameAsync(targetUserId);
        await AddSystemMessageAsync(groupId, requesterId, $"{actorName} đã xóa {targetName} khỏi nhóm");
        var payload = new { groupId, userId = targetUserId, message = "Bạn đã bị xóa khỏi nhóm này." };

        foreach (var connectionId in _connectionTracker.GetConnections(targetUserId))
        {
            await _hub.Groups.RemoveFromGroupAsync(connectionId, $"group_{groupId}");
        }

        await _hub.Clients.Group($"group_{groupId}").SendAsync("GroupMemberKicked", payload);
        await _hub.Clients.Group($"user_{targetUserId}").SendAsync("GroupMemberKicked", payload);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateMemberRoleAsync(
        int groupId, int requesterId, int targetUserId, string role)
    {
        var requesterRole = await GetRoleAsync(groupId, requesterId);
        if (requesterRole is null) return (false, "Bạn không còn là thành viên của nhóm này.");
        if (requesterRole != GroupMemberRole.Admin) return (false, "Chỉ admin mới được đổi role.");

        var targetMember = await _db.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == targetUserId);
        if (targetMember is null) return (false, "User không trong nhóm.");

        if (!Enum.TryParse<GroupMemberRole>(role, true, out var newRole))
            return (false, "Role không hợp lệ.");

        var oldRole = targetMember.Role;
        targetMember.Role = newRole;
        await _db.SaveChangesAsync();
        if (oldRole != newRole)
        {
            await AddSystemMessageAsync(
                groupId,
                requesterId,
                $"{await GetDisplayNameAsync(requesterId)} đã đổi vai trò {await GetDisplayNameAsync(targetUserId)} thành {newRole}");
        }
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> LeaveGroupAsync(int groupId, int userId)
    {
        var member = await _db.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        if (member is null) return (false, "Bạn không còn là thành viên của nhóm này.");

        if (member.Role == GroupMemberRole.Admin)
        {
            var nextAdmin = await _db.GroupMembers
                .Where(gm => gm.GroupId == groupId && gm.UserId != userId)
                .OrderBy(gm => gm.JoinedAt)
                .FirstOrDefaultAsync();

            if (nextAdmin != null)
            {
                nextAdmin.Role = GroupMemberRole.Admin;
            }
            else
            {
                var group = await _db.Groups.FindAsync(groupId);
                if (group != null)
                {
                    group.IsDeleted = true;
                }
            }
        }

        _db.GroupMembers.Remove(member);
        await _db.SaveChangesAsync();
        await AddSystemMessageAsync(groupId, userId, $"{await GetDisplayNameAsync(userId)} đã rời nhóm");
        return (true, null);
    }

    public async Task<(bool Success, string? Error, string? AvatarUrl)> UploadGroupAvatarAsync(
        int groupId, int userId, IFormFile file, string webRootPath)
    {
        var group = await _db.Groups.FindAsync(groupId);
        if (group is null) return (false, "Nhóm không tồn tại.", null);

        var role = await GetRoleAsync(groupId, userId);
        if (role is null) return (false, "Bạn không còn là thành viên của nhóm này.", null);
        if (role == GroupMemberRole.Member) return (false, "Không có quyền.", null);

        if (file is null || file.Length <= 0)
            return (false, "Không có file ảnh.", null);

        if (file.Length > MaxGroupAvatarSize)
            return (false, "Ảnh nhóm tối đa 5MB.", null);

        var contentType = file.ContentType?.Trim() ?? string.Empty;
        if (!AllowedAvatarContentTypes.Contains(contentType))
            return (false, "Chỉ chấp nhận file JPEG, PNG, WebP.", null);

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedAvatarExtensions.Contains(extension))
            return (false, "Chỉ hỗ trợ ảnh JPG, PNG hoặc WebP.", null);

        var dir = Path.Combine(webRootPath, "uploads", "groups");
        Directory.CreateDirectory(dir);
        var fileName = $"group_{groupId}_{Guid.NewGuid():N}.jpg";
        var path = Path.Combine(dir, fileName);

        try
        {
            await using var stream = file.OpenReadStream();
            using var image = await Image.LoadAsync(stream);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(256, 256),
                Mode = ResizeMode.Crop
            }));
            await image.SaveAsJpegAsync(path);
        }
        catch (UnknownImageFormatException)
        {
            return (false, "Định dạng ảnh không được hỗ trợ.", null);
        }
        catch (IOException)
        {
            return (false, "Không thể lưu ảnh nhóm.", null);
        }

        group.AvatarUrl = $"/uploads/groups/{fileName}";
        group.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await AddSystemMessageAsync(groupId, userId, $"{await GetDisplayNameAsync(userId)} đã đổi avatar nhóm");

        return (true, null, group.AvatarUrl);
    }

    // ─── Helpers ───────────────────────────────────────────────────────────

    private async Task<GroupMemberRole?> GetRoleAsync(int groupId, int userId)
    {
        var member = await _db.GroupMembers.FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        return member?.Role;
    }
    private async Task<bool> AreFriendsAsync(int userId, int otherUserId)
    {
        return await _db.Friendships.AnyAsync(f =>
            f.Status == FriendshipStatus.Accepted &&
            ((f.UserAId == userId && f.UserBId == otherUserId) ||
             (f.UserAId == otherUserId && f.UserBId == userId)));
    }

    private async Task<string> GetDisplayNameAsync(int userId)
    {
        return await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync() ?? "Người dùng";
    }

    private async Task<MessageResponse?> AddSystemMessageAsync(int groupId, int senderId, string content)
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

        var response = await _db.Messages
            .AsNoTracking()
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == message.Id);

        if (response is null)
            return null;

        var dto = new MessageResponse
        {
            Id = response.Id,
            SenderId = response.SenderId,
            SenderDisplayName = response.Sender.DisplayName,
            SenderAvatarUrl = response.Sender.AvatarUrl,
            GroupId = response.GroupId,
            ReceiverId = response.ReceiverId,
            Content = response.Content,
            MessageType = response.MessageType.ToString(),
            IsDeleted = response.IsDeleted,
            IsEdited = response.IsEdited,
            IsPinned = response.IsPinned,
            CreatedAt = response.CreatedAt,
            UpdatedAt = response.UpdatedAt
        };

        await _hub.Clients.Group($"group_{groupId}").SendAsync("ReceiveMessage", dto);
        return dto;
    }

    private async Task<GroupResponse?> BuildGroupResponseAsync(int groupId)
    {
        var group = await _db.Groups
            .Include(g => g.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted);

        if (group is null) return null;

        return new GroupResponse
        {
            Id              = group.Id,
            Name            = group.Name,
            Description     = group.Description,
            AvatarUrl       = group.AvatarUrl,
            CreatedByUserId = group.CreatedByUserId,
            CreatedAt       = group.CreatedAt,
            Members         = group.Members.Select(m => new GroupMemberResponse
            {
                UserId      = m.UserId,
                DisplayName = m.User.DisplayName,
                Username    = m.User.Username,
                AvatarUrl   = m.User.AvatarUrl,
                IsOnline    = m.User.IsOnline,
                Role        = m.Role.ToString(),
                JoinedAt    = m.JoinedAt
            }).ToList()
        };
    }
}
