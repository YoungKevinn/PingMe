using PingMe.DTOs.Friend;

namespace PingMe.Services;

public interface IFriendService
{
    Task<(bool Success, string? Error)> SendRequestAsync(int fromUserId, int toUserId);
    Task<(bool Success, string? Error)> RespondRequestAsync(int requestId, int userId, bool accept);
    Task<(bool Success, string? Error)> CancelRequestAsync(int requestId, int userId);
    Task<(bool Success, string? Error)> UnfriendAsync(int userId, int friendId);
    Task<List<FriendRequestResponse>> GetIncomingRequestsAsync(int userId);
    Task<List<FriendRequestResponse>> GetOutgoingRequestsAsync(int userId);
    Task<List<FriendUserResponse>> GetFriendsAsync(int userId);
    Task<bool> AreFriendsAsync(int userId, int otherId);
}
