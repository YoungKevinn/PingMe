using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs.Friend;
using PingMe.Models;

namespace PingMe.Services;

public class FriendService : IFriendService
{
    private readonly AppDbContext _db;
    public FriendService(AppDbContext db) { _db = db; }

    public async Task<(bool Success, string? Error)> SendRequestAsync(int fromUserId, int toUserId)
    {
        if (fromUserId == toUserId) return (false, "Không thể gửi lời mời cho chính mình.");

        var targetExists = await _db.Users.AnyAsync(u => u.Id == toUserId);
        if (!targetExists) return (false, "Người dùng không tồn tại.");

        var alreadyFriends = await _db.Friendships.AnyAsync(f =>
            (f.UserAId == fromUserId && f.UserBId == toUserId) ||
            (f.UserAId == toUserId && f.UserBId == fromUserId));
        if (alreadyFriends) return (false, "Hai bạn đã là bạn bè.");

        // Tìm request hiện có (theo BẤT KỲ trạng thái nào) vì có UNIQUE index trên (FromUserId, ToUserId)
        var existing = await _db.FriendRequests.FirstOrDefaultAsync(r =>
            r.FromUserId == fromUserId && r.ToUserId == toUserId);

        if (existing != null && existing.Status == FriendRequestStatus.Pending)
            return (false, "Bạn đã gửi lời mời trước đó.");

        // Nếu đối phương cũng đã gửi pending cho mình → tự động accept luôn (giống Facebook)
        var reverse = await _db.FriendRequests.FirstOrDefaultAsync(r =>
            r.FromUserId == toUserId &&
            r.ToUserId == fromUserId &&
            r.Status == FriendRequestStatus.Pending);

        try
        {
            if (reverse != null)
            {
                reverse.Status = FriendRequestStatus.Accepted;

                var a1 = Math.Min(fromUserId, toUserId);
                var b1 = Math.Max(fromUserId, toUserId);

                // Đảm bảo chưa có row Friendship nào (tránh vi phạm unique)
                var friendshipExists = await _db.Friendships.AnyAsync(f =>
                    f.UserAId == a1 && f.UserBId == b1);

                if (!friendshipExists)
                {
                    _db.Friendships.Add(new Friendship
                    {
                        UserAId = a1,
                        UserBId = b1,
                        Status = FriendshipStatus.Accepted
                    });
                }

                // Nếu đã có row của mình từ lần trước (Rejected) thì xoá để không gây trùng
                if (existing != null) _db.FriendRequests.Remove(existing);

                await _db.SaveChangesAsync();
                return (true, null);
            }

            if (existing != null)
            {
                // Tái sử dụng row cũ thay vì insert mới (tránh unique violation)
                existing.Status = FriendRequestStatus.Pending;
                existing.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.FriendRequests.Add(new FriendRequest
                {
                    FromUserId = fromUserId,
                    ToUserId = toUserId,
                    Status = FriendRequestStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;
            return (false, $"Lỗi DB: {inner}");
        }
    }

    public async Task<(bool Success, string? Error)> RespondRequestAsync(int requestId, int userId, bool accept)
    {
        var req = await _db.FriendRequests.FindAsync(requestId);
        if (req == null) return (false, "Không tìm thấy lời mời.");
        if (req.ToUserId != userId) return (false, "Bạn không có quyền với lời mời này.");
        if (req.Status != FriendRequestStatus.Pending) return (false, "Lời mời đã được xử lý.");

        req.Status = accept ? FriendRequestStatus.Accepted : FriendRequestStatus.Rejected;

        if (accept)
        {
            var a = Math.Min(req.FromUserId, req.ToUserId);
            var b = Math.Max(req.FromUserId, req.ToUserId);
            _db.Friendships.Add(new Friendship
            {
                UserAId = a,
                UserBId = b,
                Status = FriendshipStatus.Accepted
            });
        }

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CancelRequestAsync(int requestId, int userId)
    {
        var req = await _db.FriendRequests.FindAsync(requestId);
        if (req == null) return (false, "Không tìm thấy lời mời.");
        if (req.FromUserId != userId) return (false, "Bạn không có quyền hủy lời mời này.");
        if (req.Status != FriendRequestStatus.Pending) return (false, "Lời mời không còn ở trạng thái chờ.");

        _db.FriendRequests.Remove(req);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UnfriendAsync(int userId, int friendId)
    {
        var friendship = await _db.Friendships.FirstOrDefaultAsync(f =>
            (f.UserAId == userId && f.UserBId == friendId) ||
            (f.UserAId == friendId && f.UserBId == userId));

        if (friendship == null) return (false, "Hai bạn chưa phải bạn bè.");

        _db.Friendships.Remove(friendship);

        // dọn các request liên quan để sau này có thể gửi lại lời mời
        var related = _db.FriendRequests.Where(r =>
            (r.FromUserId == userId && r.ToUserId == friendId) ||
            (r.FromUserId == friendId && r.ToUserId == userId));
        _db.FriendRequests.RemoveRange(related);

        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<FriendRequestResponse>> GetIncomingRequestsAsync(int userId)
    {
        return await _db.FriendRequests
            .AsNoTracking()
            .Where(r => r.ToUserId == userId && r.Status == FriendRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .Join(_db.Users, r => r.FromUserId, u => u.Id, (r, fromUser) => new { r, fromUser })
            .Join(_db.Users, x => x.r.ToUserId, u => u.Id, (x, toUser) => new FriendRequestResponse
            {
                Id = x.r.Id,
                FromUserId = x.r.FromUserId,
                FromUserName = x.fromUser.DisplayName,
                FromUserAvatar = x.fromUser.AvatarUrl,
                ToUserId = x.r.ToUserId,
                ToUserName = toUser.DisplayName,
                Status = x.r.Status.ToString(),
                CreatedAt = x.r.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<FriendRequestResponse>> GetOutgoingRequestsAsync(int userId)
    {
        return await _db.FriendRequests
            .AsNoTracking()
            .Where(r => r.FromUserId == userId && r.Status == FriendRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .Join(_db.Users, r => r.FromUserId, u => u.Id, (r, fromUser) => new { r, fromUser })
            .Join(_db.Users, x => x.r.ToUserId, u => u.Id, (x, toUser) => new FriendRequestResponse
            {
                Id = x.r.Id,
                FromUserId = x.r.FromUserId,
                FromUserName = x.fromUser.DisplayName,
                FromUserAvatar = x.fromUser.AvatarUrl,
                ToUserId = x.r.ToUserId,
                ToUserName = toUser.DisplayName,
                Status = x.r.Status.ToString(),
                CreatedAt = x.r.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<FriendUserResponse>> GetFriendsAsync(int userId)
    {
        var friendIds = await _db.Friendships
            .AsNoTracking()
            .Where(f => (f.UserAId == userId || f.UserBId == userId) && f.Status == FriendshipStatus.Accepted)
            .Select(f => f.UserAId == userId ? f.UserBId : f.UserAId)
            .ToListAsync();

        if (friendIds.Count == 0) return new List<FriendUserResponse>();

        return await _db.Users
            .AsNoTracking()
            .Where(u => friendIds.Contains(u.Id))
            .OrderByDescending(u => u.IsOnline)
            .ThenBy(u => u.DisplayName)
            .Select(u => new FriendUserResponse
            {
                UserId = u.Id,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl,
                IsOnline = u.IsOnline,
                LastSeen = u.LastSeen
            })
            .ToListAsync();
    }

    public async Task<bool> AreFriendsAsync(int userId, int otherId)
    {
        return await _db.Friendships.AnyAsync(f =>
            ((f.UserAId == userId && f.UserBId == otherId) ||
             (f.UserAId == otherId && f.UserBId == userId)) &&
            f.Status == FriendshipStatus.Accepted);
    }
}
