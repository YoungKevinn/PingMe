using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs.Message;
using PingMe.DTOs.Poll;
using PingMe.Hubs;
using PingMe.Models;

namespace PingMe.Services;

public interface IPollService
{
    Task<(bool Success, string? Error, MessageResponse? Message)> CreatePollAsync(int userId, CreatePollRequest request);
    Task<(bool Success, string? Error)> VoteAsync(int pollId, int userId, PollVoteRequest request);
    Task<(bool Success, string? Error)> UnvoteAsync(int pollId, int userId);
    Task<PollResponse?> GetPollResponseAsync(int pollId, int currentUserId);
}

public class PollService : IPollService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ChatHub> _hub;

    public PollService(AppDbContext db, IHubContext<ChatHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<(bool Success, string? Error, MessageResponse? Message)> CreatePollAsync(int userId, CreatePollRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return (false, "Câu hỏi không được để trống.", null);

        if (request.Options == null || request.Options.Count < 2)
            return (false, "Cần ít nhất 2 lựa chọn.", null);

        if (request.Options.Count > 10)
            return (false, "Tối đa 10 lựa chọn.", null);

        if (request.ReceiverId == null && request.GroupId == null)
            return (false, "Cần chỉ định người nhận hoặc nhóm.", null);

        if (request.GroupId.HasValue)
        {
            var isMember = await _db.GroupMembers.AnyAsync(gm =>
                gm.GroupId == request.GroupId.Value && gm.UserId == userId && !gm.Group.IsDeleted);
            if (!isMember)
                return (false, "Bạn không phải thành viên nhóm này.", null);
        }

        var message = new Message
        {
            SenderId = userId,
            ReceiverId = request.ReceiverId,
            GroupId = request.GroupId,
            Content = request.Question.Trim(),
            MessageType = MessageType.Poll,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var poll = new Poll
        {
            MessageId = message.Id,
            AllowMultiple = request.AllowMultiple,
            EndsAt = request.EndsAt?.ToUniversalTime(),
            CreatedAt = DateTime.UtcNow,
        };

        _db.Polls.Add(poll);
        await _db.SaveChangesAsync();

        var options = request.Options
            .Select((text, i) => new PollOption
            {
                PollId = poll.Id,
                Text = text.Trim(),
                Order = i,
            })
            .ToList();

        _db.PollOptions.AddRange(options);
        await _db.SaveChangesAsync();

        var sender = await _db.Users.FindAsync(userId);
        var pollResponse = await BuildPollResponseAsync(poll.Id, userId);

        var msgResponse = new MessageResponse
        {
            Id = message.Id,
            SenderId = userId,
            SenderDisplayName = sender?.DisplayName ?? sender?.Username ?? string.Empty,
            SenderAvatarUrl = sender?.AvatarUrl,
            GroupId = message.GroupId,
            ReceiverId = message.ReceiverId,
            Content = message.Content,
            MessageType = "Poll",
            IsDeleted = false,
            IsEdited = false,
            IsPinned = false,
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            Attachments = [],
            Reactions = [],
            ReadByUserIds = [],
            Poll = pollResponse,
        };

        // Broadcast via SignalR
        if (message.GroupId.HasValue)
        {
            await _hub.Clients.Group($"group_{message.GroupId.Value}")
                .SendAsync("ReceiveMessage", msgResponse);
        }
        else if (message.ReceiverId.HasValue)
        {
            await _hub.Clients.Group($"user_{userId}")
                .SendAsync("ReceiveMessage", msgResponse);
            await _hub.Clients.Group($"user_{message.ReceiverId.Value}")
                .SendAsync("ReceiveMessage", msgResponse);
        }

        return (true, null, msgResponse);
    }

    public async Task<(bool Success, string? Error)> VoteAsync(int pollId, int userId, PollVoteRequest request)
    {
        var poll = await _db.Polls
            .Include(p => p.Options)
            .Include(p => p.Message)
            .FirstOrDefaultAsync(p => p.Id == pollId);

        if (poll is null)
            return (false, "Poll không tồn tại.");

        if (poll.EndsAt.HasValue && poll.EndsAt.Value < DateTime.UtcNow)
            return (false, "Poll đã kết thúc.");

        if (!await CanAccessPollAsync(userId, poll))
            return (false, "Bạn không có quyền vote poll này.");

        if (request.OptionIds == null || request.OptionIds.Count == 0)
            return (false, "Chọn ít nhất 1 đáp án.");

        if (!poll.AllowMultiple && request.OptionIds.Count > 1)
            return (false, "Poll này chỉ cho phép chọn 1 đáp án.");

        var validOptionIds = poll.Options.Select(o => o.Id).ToHashSet();
        if (request.OptionIds.Any(id => !validOptionIds.Contains(id)))
            return (false, "Lựa chọn không hợp lệ.");

        // Remove existing votes from this user on this poll
        var existingVotes = await _db.PollVotes
            .Where(v => request.OptionIds.Contains(v.PollOptionId) == false
                && poll.Options.Select(o => o.Id).Contains(v.PollOptionId)
                && v.UserId == userId)
            .ToListAsync();

        // Actually get all votes by this user across all options of this poll
        var allUserVotes = await _db.PollVotes
            .Where(v => v.UserId == userId && poll.Options.Select(o => o.Id).Contains(v.PollOptionId))
            .ToListAsync();

        _db.PollVotes.RemoveRange(allUserVotes);

        var newVotes = request.OptionIds.Select(optId => new PollVote
        {
            PollOptionId = optId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
        }).ToList();

        _db.PollVotes.AddRange(newVotes);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (false, "Lỗi khi lưu vote.");
        }

        await BroadcastPollUpdateAsync(poll, userId);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UnvoteAsync(int pollId, int userId)
    {
        var poll = await _db.Polls
            .Include(p => p.Options)
            .Include(p => p.Message)
            .FirstOrDefaultAsync(p => p.Id == pollId);

        if (poll is null)
            return (false, "Poll không tồn tại.");

        if (!await CanAccessPollAsync(userId, poll))
            return (false, "Bạn không có quyền bỏ vote poll này.");

        var optionIds = poll.Options.Select(o => o.Id).ToList();
        var votes = await _db.PollVotes
            .Where(v => v.UserId == userId && optionIds.Contains(v.PollOptionId))
            .ToListAsync();

        _db.PollVotes.RemoveRange(votes);
        await _db.SaveChangesAsync();

        await BroadcastPollUpdateAsync(poll, userId);
        return (true, null);
    }

    public async Task<PollResponse?> GetPollResponseAsync(int pollId, int currentUserId)
    {
        return await BuildPollResponseAsync(pollId, currentUserId);
    }

    private async Task<PollResponse?> BuildPollResponseAsync(int pollId, int currentUserId)
    {
        var poll = await _db.Polls
            .AsNoTracking()
            .Include(p => p.Options)
                .ThenInclude(o => o.Votes)
            .Include(p => p.Message)
            .FirstOrDefaultAsync(p => p.Id == pollId);

        if (poll is null) return null;

        var totalVotes = poll.Options.SelectMany(o => o.Votes).Select(v => v.UserId).Distinct().Count();
        var myVoteOptionIds = poll.Options
            .Where(o => o.Votes.Any(v => v.UserId == currentUserId))
            .Select(o => o.Id)
            .ToList();

        var options = poll.Options
            .OrderBy(o => o.Order)
            .Select(o =>
            {
                var voteCount = o.Votes.Count;
                return new PollOptionResponse
                {
                    Id = o.Id,
                    Text = o.Text,
                    Order = o.Order,
                    VoteCount = voteCount,
                    Percentage = totalVotes > 0 ? Math.Round(voteCount * 100.0 / totalVotes, 1) : 0,
                    VoterIds = o.Votes.Select(v => v.UserId).ToList(),
                };
            })
            .ToList();

        return new PollResponse
        {
            Id = poll.Id,
            MessageId = poll.MessageId,
            Question = poll.Message.Content ?? string.Empty,
            AllowMultiple = poll.AllowMultiple,
            EndsAt = poll.EndsAt,
            TotalVotes = totalVotes,
            MyVoteOptionIds = myVoteOptionIds,
            Options = options,
        };
    }

    private async Task BroadcastPollUpdateAsync(Poll poll, int actorUserId)
    {
        var pollResponse = await BuildPollResponseAsync(poll.Id, actorUserId);
        if (pollResponse is null) return;

        var payload = new PollVoteUpdatedEvent
        {
            MessageId = poll.MessageId,
            GroupId = poll.Message.GroupId,
            ReceiverId = poll.Message.ReceiverId,
            SenderId = poll.Message.SenderId,
            Poll = pollResponse,
        };

        if (poll.Message.GroupId.HasValue)
        {
            await _hub.Clients.Group($"group_{poll.Message.GroupId.Value}")
                .SendAsync("PollVoteUpdated", payload);
        }
        else
        {
            await _hub.Clients.Group($"user_{poll.Message.SenderId}")
                .SendAsync("PollVoteUpdated", payload);
            if (poll.Message.ReceiverId.HasValue)
            {
                await _hub.Clients.Group($"user_{poll.Message.ReceiverId.Value}")
                    .SendAsync("PollVoteUpdated", payload);
            }
        }
    }

    private async Task<bool> CanAccessPollAsync(int userId, Poll poll)
    {
        if (poll.Message.GroupId.HasValue)
        {
            return await _db.GroupMembers.AnyAsync(gm =>
                gm.GroupId == poll.Message.GroupId.Value &&
                gm.UserId == userId &&
                !gm.Group.IsDeleted);
        }

        return poll.Message.SenderId == userId || poll.Message.ReceiverId == userId;
    }
}
