using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs;
using PingMe.Hubs;
using PingMe.Models;

namespace PingMe.Services;

public interface IGroupTaskService
{
    Task<GroupTaskListResponseDto> GetAsync(int currentUserId, GroupTaskQueryDto query);
    Task<int> GetOverdueCountAsync(int currentUserId);
    Task<GroupTaskResponseDto?> GetByIdAsync(int currentUserId, int id);
    Task<(GroupTaskResponseDto? Task, string? Error)> CreateAsync(int currentUserId, CreateGroupTaskDto dto);
    Task<(GroupTaskResponseDto? Task, string? Error)> UpdateAsync(int currentUserId, int id, UpdateGroupTaskDto dto);
    Task<(GroupTaskResponseDto? Task, string? Error)> CompleteAsync(int currentUserId, int id, bool isCompleted);
    Task<(bool Success, string? Error)> DeleteAsync(int currentUserId, int id);
    Task<string?> ValidateCommandCreateAsync(int currentUserId, string rawCommand, int? groupId, int? receiverId);
    Task<(GroupTaskResponseDto? Task, string? Error)> CreateFromCommandAsync(int currentUserId, string rawCommand, int groupId);
    Task AttachSourceMessageAsync(int taskId, int messageId);
    Task BroadcastTaskAssignedAsync(GroupTaskResponseDto task);
}

public class GroupTaskService : IGroupTaskService
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 1000;
    private const string OpenStatus = "Open";
    private const string DoneStatus = "Done";
    private const string CancelledStatus = "Cancelled";

    private static readonly TimeZoneInfo VietnamTz = LoadVietnamTz();

    private static TimeZoneInfo LoadVietnamTz()
    {
        foreach (var id in new[] { "SE Asia Standard Time", "Asia/Ho_Chi_Minh", "Asia/Saigon" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch { }
        }
        return TimeZoneInfo.CreateCustomTimeZone("VN+7", TimeSpan.FromHours(7), "Vietnam Standard Time", "Vietnam Standard Time");
    }

    private static readonly Regex TaskCommandRegex = new(
        @"^\s*/task\s+(?<rest>[\s\S]+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DueTokenRegex = new(
        @"(^|\s)due:(?<value>\S+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PriorityTokenRegex = new(
        @"(^|\s)priority:(?<value>\S+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Lazy match: dừng trước due:/priority:/@token hoặc end-of-string
    // → hỗ trợ cả @alice (username) lẫn @Ngoc Huong (display name có space)
    private static readonly Regex MentionTokenRegex = new(
        @"(^|\s)@(?<username>[A-Za-z0-9_.][A-Za-z0-9_. ]*?)(?=\s+(?:due:|priority:|@[A-Za-z])|\s*$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly IHubContext<ChatHub> _hub;

    public GroupTaskService(AppDbContext db, IHubContext<ChatHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<GroupTaskListResponseDto> GetAsync(int currentUserId, GroupTaskQueryDto query)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize <= 0 ? 20 : query.PageSize, 1, 50);
        var dbQuery = VisibleTasks(currentUserId);

        if (query.GroupId.HasValue)
            dbQuery = dbQuery.Where(t => t.GroupId == query.GroupId.Value);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            dbQuery = dbQuery.Where(t =>
                t.Title.Contains(keyword) ||
                (t.Description != null && t.Description.Contains(keyword)) ||
                t.Priority.Contains(keyword) ||
                t.Status.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            try
            {
                var normalizedStatus = NormalizeStatus(query.Status);
                dbQuery = dbQuery.Where(t => t.Status == normalizedStatus);
            }
            catch (ArgumentException)
            {
                // Invalid status filter → return empty set
                return new GroupTaskListResponseDto { Items = [], Total = 0, Page = page, PageSize = pageSize };
            }
        }

        if (!string.IsNullOrWhiteSpace(query.Priority))
            dbQuery = dbQuery.Where(t => t.Priority == NormalizePriority(query.Priority));

        if (query.AssignedToMe)
            dbQuery = dbQuery.Where(t => t.AssignedToUserId == currentUserId);

        if (query.Overdue)
            dbQuery = dbQuery.Where(t =>
                t.DueAtUtc.HasValue &&
                t.DueAtUtc.Value < DateTime.UtcNow &&
                t.Status != DoneStatus &&
                t.Status != CancelledStatus);

        var total = await dbQuery.CountAsync();
        var items = await dbQuery
            .OrderBy(t => t.Status == DoneStatus || t.Status == CancelledStatus)
            .ThenBy(t => t.DueAtUtc ?? DateTime.MaxValue)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(TaskProjection)
            .ToListAsync();

        return new GroupTaskListResponseDto
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<int> GetOverdueCountAsync(int currentUserId)
    {
        var now = DateTime.UtcNow;
        return await VisibleTasks(currentUserId)
            .Where(t =>
                t.DueAtUtc.HasValue &&
                t.DueAtUtc.Value < now &&
                t.Status != DoneStatus &&
                t.Status != CancelledStatus)
            .CountAsync();
    }

    public async Task<GroupTaskResponseDto?> GetByIdAsync(int currentUserId, int id)
    {
        return await VisibleTasks(currentUserId)
            .Where(t => t.Id == id)
            .Select(TaskProjection)
            .FirstOrDefaultAsync();
    }

    public async Task<(GroupTaskResponseDto? Task, string? Error)> CreateAsync(int currentUserId, CreateGroupTaskDto dto)
    {
        var validation = await ValidateCreateAsync(currentUserId, dto);
        if (!validation.Success)
            return (null, validation.Error);

        var now = DateTime.UtcNow;
        var task = new GroupTask
        {
            GroupId = dto.GroupId,
            Title = dto.Title.Trim(),
            Description = NormalizeDescription(dto.Description),
            Priority = NormalizePriority(dto.Priority),
            Status = OpenStatus,
            DueAtUtc = dto.DueAtUtc,
            CreatedByUserId = currentUserId,
            AssignedToUserId = dto.AssignedToUserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.GroupTasks.Add(task);
        await _db.SaveChangesAsync();

        var response = await GetByIdAsync(currentUserId, task.Id);
        if (response is not null)
            await BroadcastTaskAssignedAsync(response);

        return (response, null);
    }

    public async Task<(GroupTaskResponseDto? Task, string? Error)> UpdateAsync(int currentUserId, int id, UpdateGroupTaskDto dto)
    {
        var task = await _db.GroupTasks.FirstOrDefaultAsync(t => t.Id == id);
        if (task is null)
            return (null, "Task không tồn tại.");

        if (!await CanUpdateAsync(currentUserId, task))
            return (null, "Bạn không có quyền cập nhật task này.");

        var previousAssignedToUserId = task.AssignedToUserId;

        if (dto.Title != null)
        {
            var title = dto.Title.Trim();
            if (string.IsNullOrWhiteSpace(title))
                return (null, "Title task không được để trống.");
            if (title.Length > MaxTitleLength)
                return (null, $"Title task tối đa {MaxTitleLength} ký tự.");
            task.Title = title;
        }

        if (dto.Description != null)
        {
            if (dto.Description.Length > MaxDescriptionLength)
                return (null, $"Mô tả task tối đa {MaxDescriptionLength} ký tự.");
            task.Description = NormalizeDescription(dto.Description);
        }

        if (dto.AssignedToUserId.HasValue)
        {
            if (!await IsGroupMemberAsync(dto.AssignedToUserId.Value, task.GroupId))
                return (null, "Người được assign không phải thành viên group.");
            task.AssignedToUserId = dto.AssignedToUserId;
        }

        if (dto.DueAtUtc.HasValue)
            task.DueAtUtc = dto.DueAtUtc;

        if (!string.IsNullOrWhiteSpace(dto.Priority))
        {
            try
            {
                task.Priority = NormalizePriority(dto.Priority);
            }
            catch (ArgumentException ex)
            {
                return (null, ex.Message);
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.Status))
        {
            try
            {
                ApplyStatus(task, NormalizeStatus(dto.Status));
            }
            catch (ArgumentException ex)
            {
                return (null, ex.Message);
            }
        }

        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var response = await GetByIdAsync(currentUserId, task.Id);
        if (response is not null &&
            response.AssignedToUserId.HasValue &&
            response.AssignedToUserId != previousAssignedToUserId)
        {
            await BroadcastTaskAssignedAsync(response);
        }

        return (response, null);
    }

    public async Task<(GroupTaskResponseDto? Task, string? Error)> CompleteAsync(int currentUserId, int id, bool isCompleted)
    {
        var task = await _db.GroupTasks.FirstOrDefaultAsync(t => t.Id == id);
        if (task is null)
            return (null, "Task không tồn tại.");

        if (!await CanUpdateAsync(currentUserId, task))
            return (null, "Bạn không có quyền hoàn tất task này.");

        ApplyStatus(task, isCompleted ? DoneStatus : OpenStatus);
        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (await GetByIdAsync(currentUserId, task.Id), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int currentUserId, int id)
    {
        var task = await _db.GroupTasks.FirstOrDefaultAsync(t => t.Id == id);
        if (task is null)
            return (false, "Task không tồn tại.");

        if (!await CanDeleteAsync(currentUserId, task))
            return (false, "Bạn không có quyền xóa task này.");

        _db.GroupTasks.Remove(task);
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<string?> ValidateCommandCreateAsync(int currentUserId, string rawCommand, int? groupId, int? receiverId)
    {
        if (receiverId.HasValue)
            return "/task chỉ hỗ trợ group chat.";

        if (!groupId.HasValue)
            return "Cần chọn group để tạo task từ /task.";

        var parsed = ParseCommand(rawCommand);
        if (!parsed.Success)
            return parsed.Error;

        var dto = new CreateGroupTaskDto
        {
            GroupId = groupId.Value,
            Title = parsed.Title,
            DueAtUtc = parsed.DueAtUtc,
            Priority = parsed.Priority
        };

        var resolved = await ResolveMentionAsync(groupId.Value, parsed.AssignedUsername);
        if (!resolved.Success)
            return resolved.Error;

        dto.AssignedToUserId = resolved.UserId;

        var validation = await ValidateCreateAsync(currentUserId, dto);
        return validation.Success ? null : validation.Error;
    }

    public async Task<(GroupTaskResponseDto? Task, string? Error)> CreateFromCommandAsync(
        int currentUserId,
        string rawCommand,
        int groupId)
    {
        var parsed = ParseCommand(rawCommand);
        if (!parsed.Success)
            return (null, parsed.Error);

        var resolved = await ResolveMentionAsync(groupId, parsed.AssignedUsername);
        if (!resolved.Success)
            return (null, resolved.Error);

        return await CreateAsync(currentUserId, new CreateGroupTaskDto
        {
            GroupId = groupId,
            Title = parsed.Title,
            AssignedToUserId = resolved.UserId,
            DueAtUtc = parsed.DueAtUtc,
            Priority = parsed.Priority
        });
    }

    public async Task AttachSourceMessageAsync(int taskId, int messageId)
    {
        var task = await _db.GroupTasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task is null)
            return;

        task.SourceMessageId = messageId;
        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task BroadcastTaskAssignedAsync(GroupTaskResponseDto task)
    {
        if (!task.AssignedToUserId.HasValue)
            return;

        var payload = new TaskAssignedEventDto
        {
            Task = task,
            GroupId = task.GroupId,
            AssignedToUserId = task.AssignedToUserId.Value
        };

        await _hub.Clients.Group($"group_{task.GroupId}").SendAsync("TaskAssigned", payload);
    }

    private IQueryable<GroupTask> VisibleTasks(int userId)
    {
        return _db.GroupTasks
            .AsNoTracking()
            .Where(t => _db.GroupMembers.Any(gm =>
                gm.GroupId == t.GroupId &&
                gm.UserId == userId &&
                !gm.Group.IsDeleted));
    }

    private async Task<(bool Success, string? Error)> ValidateCreateAsync(int currentUserId, CreateGroupTaskDto dto)
    {
        var title = dto.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
            return (false, "Title task không được để trống.");

        if (title.Length > MaxTitleLength)
            return (false, $"Title task tối đa {MaxTitleLength} ký tự.");

        if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description.Length > MaxDescriptionLength)
            return (false, $"Mô tả task tối đa {MaxDescriptionLength} ký tự.");

        if (dto.GroupId <= 0)
            return (false, "GroupId không hợp lệ.");

        if (!await IsGroupMemberAsync(currentUserId, dto.GroupId))
            return (false, "Bạn không phải thành viên group này.");

        if (dto.AssignedToUserId.HasValue && !await IsGroupMemberAsync(dto.AssignedToUserId.Value, dto.GroupId))
            return (false, "Người được assign không phải thành viên group.");

        try
        {
            _ = NormalizePriority(dto.Priority);
        }
        catch (ArgumentException ex)
        {
            return (false, ex.Message);
        }

        return (true, null);
    }

    private async Task<bool> CanUpdateAsync(int userId, GroupTask task)
    {
        if (task.CreatedByUserId == userId || task.AssignedToUserId == userId)
            return true;

        return await IsAdminOrCoAdminAsync(userId, task.GroupId);
    }

    private async Task<bool> CanDeleteAsync(int userId, GroupTask task)
    {
        if (task.CreatedByUserId == userId)
            return true;

        return await IsAdminOrCoAdminAsync(userId, task.GroupId);
    }

    private async Task<bool> IsGroupMemberAsync(int userId, int groupId)
    {
        return await _db.GroupMembers
            .AsNoTracking()
            .AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId && !gm.Group.IsDeleted);
    }

    private async Task<bool> IsAdminOrCoAdminAsync(int userId, int groupId)
    {
        return await _db.GroupMembers
            .AsNoTracking()
            .AnyAsync(gm =>
                gm.GroupId == groupId &&
                gm.UserId == userId &&
                (gm.Role == GroupMemberRole.Admin || gm.Role == GroupMemberRole.CoAdmin));
    }

    private async Task<(bool Success, int? UserId, string? Error)> ResolveMentionAsync(int groupId, string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return (true, null, null);

        var normalized = username.Trim().TrimStart('@').ToLower();

        // Ưu tiên match chính xác username (case-insensitive)
        var byUsername = await _db.GroupMembers
            .AsNoTracking()
            .Where(gm => gm.GroupId == groupId && gm.User.Username.ToLower() == normalized)
            .Select(gm => new { gm.UserId, gm.User.Username, gm.User.DisplayName })
            .FirstOrDefaultAsync();

        if (byUsername != null)
            return (true, byUsername.UserId, null);

        // Nếu không tìm được qua username, thử DisplayName (case-insensitive)
        var byDisplayName = await _db.GroupMembers
            .AsNoTracking()
            .Where(gm => gm.GroupId == groupId && gm.User.DisplayName.ToLower() == normalized)
            .Select(gm => new { gm.UserId, gm.User.Username, gm.User.DisplayName })
            .ToListAsync();

        if (byDisplayName.Count == 0)
            return (false, null, $"Không tìm thấy @{username.Trim().TrimStart('@')} trong group.");

        if (byDisplayName.Count > 1)
            return (false, null, $"Có nhiều user trùng tên \"{username.Trim().TrimStart('@')}\", hãy dùng username thay thế.");

        return (true, byDisplayName[0].UserId, null);
    }

    private static ParsedTaskCommand ParseCommand(string? rawCommand)
    {
        if (string.IsNullOrWhiteSpace(rawCommand))
            return ParsedTaskCommand.Fail("Command /task rỗng.");

        var match = TaskCommandRegex.Match(rawCommand.Trim());
        if (!match.Success)
            return ParsedTaskCommand.Fail("Cú pháp /task không hợp lệ. Ví dụ: /task Review login flow @alice due:tomorrow priority:high");

        var rest = match.Groups["rest"].Value.Trim();
        string? assignedUsername = null;
        DateTime? dueAtUtc = null;
        var priority = "Medium";
        string? error = null;

        rest = MentionTokenRegex.Replace(rest, m =>
        {
            var username = m.Groups["username"].Value.Trim();
            if (assignedUsername != null)
                error = "/task chỉ hỗ trợ assign một user.";
            assignedUsername ??= username;
            return " ";
        });

        if (error != null)
            return ParsedTaskCommand.Fail(error);

        rest = DueTokenRegex.Replace(rest, m =>
        {
            var value = m.Groups["value"].Value.Trim();
            var parsed = ParseDueDate(value);
            if (!parsed.Success)
                error = parsed.Error;
            else
                dueAtUtc = parsed.DueAtUtc;
            return " ";
        });

        if (error != null)
            return ParsedTaskCommand.Fail(error);

        rest = PriorityTokenRegex.Replace(rest, m =>
        {
            var value = m.Groups["value"].Value.Trim();
            try
            {
                priority = NormalizePriority(value);
            }
            catch (ArgumentException ex)
            {
                error = ex.Message;
            }
            return " ";
        });

        if (error != null)
            return ParsedTaskCommand.Fail(error);

        var title = Regex.Replace(rest, @"\s+", " ").Trim();
        if (string.IsNullOrWhiteSpace(title))
            return ParsedTaskCommand.Fail("Title task không được để trống.");

        if (title.Length > MaxTitleLength)
            return ParsedTaskCommand.Fail($"Title task tối đa {MaxTitleLength} ký tự.");

        return new ParsedTaskCommand
        {
            Success = true,
            Title = title,
            AssignedUsername = assignedUsername,
            DueAtUtc = dueAtUtc,
            Priority = priority
        };
    }

    private static (bool Success, DateTime? DueAtUtc, string? Error) ParseDueDate(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();

        // Lấy ngày hiện tại theo múi giờ Việt Nam (UTC+7) để tránh lệch ngày
        var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VietnamTz);
        var todayVn = nowVn.Date;

        if (normalized == "today")
            return (true, EndOfDayVn(todayVn), null);

        if (normalized == "tomorrow")
            return (true, EndOfDayVn(todayVn.AddDays(1)), null);

        if (TryParseWeekday(normalized, out var targetDay))
        {
            var diff = ((int)targetDay - (int)todayVn.DayOfWeek + 7) % 7;
            if (diff == 0) diff = 7; // nếu hôm nay là đúng ngày đó → chuyển sang tuần sau
            return (true, EndOfDayVn(todayVn.AddDays(diff)), null);
        }

        if (DateOnly.TryParseExact(normalized, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            // Ngày cụ thể: tính end-of-day theo VN rồi convert UTC
            return (true, EndOfDayVn(date.ToDateTime(TimeOnly.MinValue)), null);
        }

        return (false, null, "due không hợp lệ. Hỗ trợ due:today, due:tomorrow, due:monday..sunday, hoặc due:yyyy-MM-dd.");
    }

    /// <summary>
    /// Nhận một DateTime là ngày local (VN), trả về end-of-day đã convert sang UTC.
    /// </summary>
    private static DateTime EndOfDayVn(DateTime localDate)
    {
        var endOfDayLocal = localDate.Date.AddDays(1).AddTicks(-1);
        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(endOfDayLocal, DateTimeKind.Unspecified),
            VietnamTz);
    }

    private static bool TryParseWeekday(string value, out DayOfWeek day)
    {
        day = value switch
        {
            "sunday" => DayOfWeek.Sunday,
            "monday" => DayOfWeek.Monday,
            "tuesday" => DayOfWeek.Tuesday,
            "wednesday" => DayOfWeek.Wednesday,
            "thursday" => DayOfWeek.Thursday,
            "friday" => DayOfWeek.Friday,
            "saturday" => DayOfWeek.Saturday,
            _ => DayOfWeek.Sunday
        };

        return value is "sunday" or "monday" or "tuesday" or "wednesday" or "thursday" or "friday" or "saturday";
    }

    private static string NormalizePriority(string? priority)
    {
        return (priority ?? "Medium").Trim().ToLowerInvariant() switch
        {
            "" => "Medium",
            "low" => "Low",
            "medium" => "Medium",
            "high" => "High",
            "critical" => "Critical",
            _ => throw new ArgumentException("priority không hợp lệ. Hỗ trợ low/medium/high/critical.")
        };
    }

    private static string NormalizeStatus(string? status)
    {
        return (status ?? OpenStatus).Trim().ToLowerInvariant() switch
        {
            "" or "open" or "todo" => OpenStatus,
            "inprogress" or "in-progress" or "doing" => "InProgress",
            "done" or "completed" or "complete" => DoneStatus,
            "cancelled" or "canceled" => CancelledStatus,
            var s => throw new ArgumentException($"Status '{s}' không hợp lệ. Hỗ trợ: Open, InProgress, Done, Cancelled.")
        };
    }

    private static string? NormalizeDescription(string? description)
        => string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static void ApplyStatus(GroupTask task, string status)
    {
        task.Status = status;
        task.CompletedAt = status == DoneStatus ? DateTime.UtcNow : null;
    }

    private static readonly Expression<Func<GroupTask, GroupTaskResponseDto>> TaskProjection = task => new GroupTaskResponseDto
    {
        Id = task.Id,
        GroupId = task.GroupId,
        GroupName = task.Group.Name,
        Title = task.Title,
        Description = task.Description,
        Priority = task.Priority,
        Status = task.Status,
        DueAtUtc = task.DueAtUtc,
        CreatedByUserId = task.CreatedByUserId,
        CreatedByDisplayName = task.CreatedByUser.DisplayName,
        AssignedToUserId = task.AssignedToUserId,
        AssignedToDisplayName = task.AssignedToUser != null ? task.AssignedToUser.DisplayName : null,
        SourceMessageId = task.SourceMessageId,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt,
        CompletedAt = task.CompletedAt
    };

    private sealed class ParsedTaskCommand
    {
        public bool Success { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? AssignedUsername { get; init; }
        public DateTime? DueAtUtc { get; init; }
        public string Priority { get; init; } = "Medium";
        public string? Error { get; init; }

        public static ParsedTaskCommand Fail(string? error) => new()
        {
            Success = false,
            Error = error
        };
    }
}

public class TaskAssignedEventDto
{
    public int GroupId { get; set; }
    public int AssignedToUserId { get; set; }
    public GroupTaskResponseDto Task { get; set; } = new();
}
