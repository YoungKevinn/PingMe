using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs;
using PingMe.Models;

namespace PingMe.Services;

public interface IReminderService
{
    Task<(ReminderDto? Reminder, string? Error)> CreateAsync(int currentUserId, CreateReminderDto dto);
    Task<List<ReminderDto>> GetMineAsync(int currentUserId, ReminderQueryDto query);
    Task<(bool Success, string? Error)> CompleteAsync(int currentUserId, int id);
    Task<(bool Success, string? Error)> CancelAsync(int currentUserId, int id);
    Task<List<ReminderDto>> GetDueRemindersAsync(DateTime nowUtc, int take);
    Task MarkSentAsync(int id);
}

public class ReminderService : IReminderService
{
    public const string Pending = "Pending";
    public const string Sent = "Sent";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";

    private readonly AppDbContext _db;

    public ReminderService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(ReminderDto? Reminder, string? Error)> CreateAsync(int currentUserId, CreateReminderDto dto)
    {
        var normalizedText = dto.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedText))
            return (null, "Nội dung nhắc việc không được để trống.");

        if (normalizedText.Length > 500)
            return (null, "Nội dung nhắc việc tối đa 500 ký tự.");

        if (dto.RemindAtUtc <= DateTime.UtcNow)
            return (null, "Thời điểm nhắc phải ở tương lai.");

        var scopeValidation = await ValidateScopeAsync(currentUserId, dto.GroupId, dto.PeerUserId);
        if (!scopeValidation.Success)
            return (null, scopeValidation.Error);

        var reminder = new ChatReminder
        {
            Text = normalizedText,
            RemindAtUtc = DateTime.SpecifyKind(dto.RemindAtUtc, DateTimeKind.Utc),
            Status = Pending,
            IsSent = false,
            CreatedByUserId = currentUserId,
            GroupId = dto.GroupId,
            PeerUserId = dto.PeerUserId,
            SourceMessageId = dto.SourceMessageId,
            CreatedAt = DateTime.UtcNow
        };

        _db.ChatReminders.Add(reminder);
        await _db.SaveChangesAsync();

        return (await GetByIdForOwnerAsync(currentUserId, reminder.Id), null);
    }

    public async Task<List<ReminderDto>> GetMineAsync(int currentUserId, ReminderQueryDto query)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var status = NormalizeStatus(query.Status);

        var reminders = _db.ChatReminders
            .AsNoTracking()
            .Include(r => r.CreatedByUser)
            .Include(r => r.Group)
            .Include(r => r.PeerUser)
            .Where(r => r.CreatedByUserId == currentUserId);

        if (!string.IsNullOrWhiteSpace(status))
            reminders = reminders.Where(r => r.Status == status);

        return await reminders
            .OrderByDescending(r => r.RemindAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => Map(r))
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> CompleteAsync(int currentUserId, int id)
    {
        var reminder = await _db.ChatReminders
            .FirstOrDefaultAsync(r => r.Id == id && r.CreatedByUserId == currentUserId);

        if (reminder is null)
            return (false, "Không tìm thấy nhắc việc.");

        reminder.Status = Completed;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CancelAsync(int currentUserId, int id)
    {
        var reminder = await _db.ChatReminders
            .FirstOrDefaultAsync(r => r.Id == id && r.CreatedByUserId == currentUserId);

        if (reminder is null)
            return (false, "Không tìm thấy nhắc việc.");

        reminder.Status = Cancelled;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<ReminderDto>> GetDueRemindersAsync(DateTime nowUtc, int take)
    {
        take = Math.Clamp(take, 1, 100);

        return await _db.ChatReminders
            .AsNoTracking()
            .Include(r => r.CreatedByUser)
            .Include(r => r.Group)
            .Include(r => r.PeerUser)
            .Where(r => r.Status == Pending && !r.IsSent && r.RemindAtUtc <= nowUtc)
            .OrderBy(r => r.RemindAtUtc)
            .Take(take)
            .Select(r => Map(r))
            .ToListAsync();
    }

    public async Task MarkSentAsync(int id)
    {
        var reminder = await _db.ChatReminders.FirstOrDefaultAsync(r => r.Id == id);
        if (reminder is null || reminder.IsSent || reminder.Status != Pending)
            return;

        reminder.IsSent = true;
        reminder.Status = Sent;
        reminder.SentAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private async Task<ReminderDto?> GetByIdForOwnerAsync(int currentUserId, int id)
    {
        var reminder = await _db.ChatReminders
            .AsNoTracking()
            .Include(r => r.CreatedByUser)
            .Include(r => r.Group)
            .Include(r => r.PeerUser)
            .FirstOrDefaultAsync(r => r.Id == id && r.CreatedByUserId == currentUserId);

        return reminder is null ? null : Map(reminder);
    }

    private async Task<(bool Success, string? Error)> ValidateScopeAsync(int currentUserId, int? groupId, int? peerUserId)
    {
        if (groupId.HasValue && peerUserId.HasValue)
            return (false, "Chỉ được tạo nhắc việc cho một DM hoặc một group.");

        if (groupId.HasValue)
        {
            var isMember = await _db.GroupMembers
                .AsNoTracking()
                .AnyAsync(gm => gm.GroupId == groupId.Value && gm.UserId == currentUserId && !gm.Group.IsDeleted);

            return isMember
                ? (true, null)
                : (false, "Bạn không còn là thành viên của nhóm này.");
        }

        if (peerUserId.HasValue)
        {
            if (peerUserId.Value == currentUserId)
                return (false, "Không thể tạo nhắc việc DM với chính mình.");

            var peerExists = await _db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == peerUserId.Value);

            return peerExists
                ? (true, null)
                : (false, "Người nhận DM không tồn tại.");
        }

        return (true, null);
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        return status.Trim().ToLowerInvariant() switch
        {
            "pending" => Pending,
            "sent" => Sent,
            "completed" => Completed,
            "cancelled" => Cancelled,
            _ => status.Trim()
        };
    }

    private static ReminderDto Map(ChatReminder reminder) => new()
    {
        Id = reminder.Id,
        Text = reminder.Text,
        RemindAtUtc = reminder.RemindAtUtc,
        Status = reminder.Status,
        IsSent = reminder.IsSent,
        CreatedByUserId = reminder.CreatedByUserId,
        CreatedByDisplayName = reminder.CreatedByUser?.DisplayName ?? string.Empty,
        GroupId = reminder.GroupId,
        GroupName = reminder.Group?.Name,
        PeerUserId = reminder.PeerUserId,
        PeerDisplayName = reminder.PeerUser?.DisplayName,
        SourceMessageId = reminder.SourceMessageId,
        CreatedAt = reminder.CreatedAt,
        SentAt = reminder.SentAt
    };
}
