using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs.Search;
using PingMe.Models;

namespace PingMe.Services;

public interface ISearchService
{
    Task<SearchResultResponse> SearchAsync(int userId, string query, int limit = 50);

    Task<List<MessageSearchResult>> SearchMessagesInConversationAsync(
        int userId,
        string query,
        int? peerId,
        int? groupId,
        int limit = 20);

    Task<GlobalSearchResponseDto> GlobalSearchAsync(int currentUserId, GlobalSearchRequestDto request);
}

public class SearchService : ISearchService
{
    private static readonly string[] ValidTypes =
    [
        "all",
        "message",
        "user",
        "group",
        "snippet",
        "ioc",
        "attachment",
        "finding",
        "task"
    ];

    private readonly AppDbContext _db;

    public SearchService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GlobalSearchResponseDto> GlobalSearchAsync(int currentUserId, GlobalSearchRequestDto request)
    {
        var keyword = (request.Keyword ?? string.Empty).Trim();
        var type = NormalizeType(request.Type);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 20 : request.PageSize, 1, 50);
        var takePerType = Math.Min(page * pageSize, 200);

        var hasSearchSignal =
            !string.IsNullOrWhiteSpace(keyword) ||
            request.FromDate.HasValue ||
            request.ToDate.HasValue ||
            request.SenderId.HasValue ||
            request.GroupId.HasValue ||
            request.PeerUserId.HasValue ||
            !string.IsNullOrWhiteSpace(request.Severity);

        if (!hasSearchSignal)
        {
            return new GlobalSearchResponseDto
            {
                Items = [],
                Total = 0,
                Page = page,
                PageSize = pageSize,
                TypeCounts = []
            };
        }

        var counts = new Dictionary<string, int>();
        var items = new List<GlobalSearchResultDto>();

        if (ShouldSearch(type, "message"))
            await AddResultsAsync("message", BuildMessageQuery(currentUserId, request, keyword), items, counts, takePerType);

        if (ShouldSearch(type, "attachment"))
            await AddResultsAsync("attachment", BuildAttachmentQuery(currentUserId, request, keyword), items, counts, takePerType);

        if (ShouldSearch(type, "user"))
            await AddResultsAsync("user", BuildUserQuery(currentUserId, request, keyword), items, counts, takePerType);

        if (ShouldSearch(type, "group"))
            await AddResultsAsync("group", BuildGroupQuery(currentUserId, request, keyword), items, counts, takePerType);

        if (ShouldSearch(type, "snippet"))
        {
            var snippets = await BuildSnippetQuery(currentUserId, request, keyword)
                .OrderByDescending(x => x.CreatedAt)
                .Take(takePerType)
                .ToListAsync();

            foreach (var snippet in snippets)
            {
                if (!string.IsNullOrWhiteSpace(snippet.Status))
                {
                    snippet.Metadata = new Dictionary<string, string>
                    {
                        ["language"] = snippet.Status
                    };
                    snippet.Status = null;
                }
            }

            counts["snippet"] = await BuildSnippetQuery(currentUserId, request, keyword).CountAsync();
            items.AddRange(snippets);
        }

        if (ShouldSearch(type, "ioc"))
            await AddResultsAsync("ioc", BuildIocQuery(currentUserId, request, keyword), items, counts, takePerType);

        if (ShouldSearch(type, "finding"))
            await AddResultsAsync("finding", BuildFindingQuery(currentUserId, request, keyword), items, counts, takePerType);

        if (ShouldSearch(type, "task"))
            await AddResultsAsync("task", BuildTaskQuery(currentUserId, request, keyword), items, counts, takePerType);

        var orderedItems = items
            .OrderByDescending(x => x.CreatedAt ?? DateTime.MinValue)
            .ThenBy(x => x.Type)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new GlobalSearchResponseDto
        {
            Items = orderedItems,
            Total = counts.Values.Sum(),
            Page = page,
            PageSize = pageSize,
            TypeCounts = counts
        };
    }

    public async Task<SearchResultResponse> SearchAsync(int userId, string query, int limit = 50)
    {
        var q = (query ?? string.Empty).Trim();
        limit = Math.Clamp(limit, 1, 100);

        var usersQuery = _db.Users
            .AsNoTracking()
            .Where(u =>
                u.Id != userId &&
                !_db.BlockedUsers.Any(b =>
                    (b.BlockerUserId == userId && b.BlockedUserId == u.Id) ||
                    (b.BlockerUserId == u.Id && b.BlockedUserId == userId)));

        if (!string.IsNullOrWhiteSpace(q))
        {
            usersQuery = usersQuery.Where(u =>
                u.Username.Contains(q) ||
                u.DisplayName.Contains(q) ||
                u.Email.Contains(q) ||
                (u.JobTitle != null && u.JobTitle.Contains(q)) ||
                (u.Department != null && u.Department.Contains(q)));
        }

        var users = await usersQuery
            .OrderByDescending(u => u.IsOnline)
            .ThenBy(u => u.DisplayName)
            .Take(limit)
            .Select(u => new UserSearchResult
            {
                Id = u.Id,
                Username = u.Username,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl,
                IsOnline = u.IsOnline,
                JobTitle = u.JobTitle,
                Department = u.Department
            })
            .ToListAsync();

        var messages = new List<MessageSearchResult>();

        if (!string.IsNullOrWhiteSpace(q))
        {
            messages = await BuildVisibleMessages(userId)
                .Where(m =>
                    !m.IsDeleted &&
                    (
                        (m.Content != null && m.Content.Contains(q)) ||
                        m.Attachments.Any(a => a.FileName.Contains(q))
                    ))
                .OrderByDescending(m => m.CreatedAt)
                .Take(limit)
                .Select(m => new MessageSearchResult
                {
                    Id = m.Id,
                    Content = m.Content,
                    SenderId = m.SenderId,
                    SenderDisplayName = m.Sender.DisplayName,
                    GroupId = m.GroupId,
                    ReceiverId = m.ReceiverId,
                    MessageType = m.MessageType.ToString(),
                    AttachmentFileName = m.Attachments
                        .OrderBy(a => a.Id)
                        .Select(a => a.FileName)
                        .FirstOrDefault(),
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();
        }

        return new SearchResultResponse
        {
            Users = users,
            Messages = messages
        };
    }

    public async Task<List<MessageSearchResult>> SearchMessagesInConversationAsync(
        int userId,
        string query,
        int? peerId,
        int? groupId,
        int limit = 20)
    {
        var q = (query ?? string.Empty).Trim();
        limit = Math.Clamp(limit, 1, 100);

        if (string.IsNullOrWhiteSpace(q))
            return [];

        IQueryable<Message> messageQuery;

        if (groupId.HasValue)
        {
            var isMember = await _db.GroupMembers
                .AsNoTracking()
                .AnyAsync(gm => gm.GroupId == groupId.Value && gm.UserId == userId);

            if (!isMember)
                return [];

            messageQuery = _db.Messages
                .AsNoTracking()
                .Where(m => m.GroupId == groupId.Value);
        }
        else if (peerId.HasValue)
        {
            messageQuery = _db.Messages
                .AsNoTracking()
                .Where(m =>
                    m.GroupId == null &&
                    (
                        (m.SenderId == userId && m.ReceiverId == peerId.Value) ||
                        (m.SenderId == peerId.Value && m.ReceiverId == userId)
                    ));
        }
        else
        {
            return [];
        }

        return await messageQuery
            .Where(m =>
                !m.IsDeleted &&
                (
                    (m.Content != null && m.Content.Contains(q)) ||
                    m.Attachments.Any(a => a.FileName.Contains(q))
                ))
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .Select(m => new MessageSearchResult
            {
                Id = m.Id,
                Content = m.Content,
                SenderId = m.SenderId,
                SenderDisplayName = m.Sender.DisplayName,
                GroupId = m.GroupId,
                ReceiverId = m.ReceiverId,
                MessageType = m.MessageType.ToString(),
                AttachmentFileName = m.Attachments
                    .OrderBy(a => a.Id)
                    .Select(a => a.FileName)
                    .FirstOrDefault(),
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();
    }

    private async Task AddResultsAsync(
        string type,
        IQueryable<GlobalSearchResultDto> query,
        List<GlobalSearchResultDto> items,
        Dictionary<string, int> counts,
        int take)
    {
        counts[type] = await query.CountAsync();

        var batch = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync();

        items.AddRange(batch);
    }

    private IQueryable<GlobalSearchResultDto> BuildMessageQuery(int userId, GlobalSearchRequestDto request, string keyword)
    {
        // Loại Task/Reminder messages vì chúng lưu JSON raw làm content →
        // show JSON rác trong search; chúng đã có category riêng (task/reminder)
        var visibleMessages = BuildVisibleMessages(userId)
            .Where(m => m.MessageType != MessageType.Task && m.MessageType != MessageType.Reminder);

        var query = ApplyMessageFilters(visibleMessages, request, keyword, includeAttachmentName: true);

        return query.Select(m => new GlobalSearchResultDto
        {
            Type = "message",
            Id = m.Id,
            MessageId = m.Id,
            Title = m.GroupId.HasValue
                ? (m.Group != null ? m.Group.Name : "Group")
                : m.Sender.DisplayName,
            Snippet = m.Content == null
                ? null
                : (m.Content.Length > 220 ? m.Content.Substring(0, 220) : m.Content),
            CreatedAt = m.CreatedAt,
            GroupId = m.GroupId,
            PeerUserId = m.GroupId == null
                ? (m.SenderId == userId ? m.ReceiverId : m.SenderId)
                : null,
            GroupName = m.Group != null ? m.Group.Name : null,
            PeerName = m.GroupId == null
                ? (m.SenderId == userId
                    ? (m.Receiver != null ? m.Receiver.DisplayName : null)
                    : m.Sender.DisplayName)
                : null,
            PeerAvatarUrl = m.GroupId == null
                ? (m.SenderId == userId
                    ? (m.Receiver != null ? m.Receiver.AvatarUrl : null)
                    : m.Sender.AvatarUrl)
                : null,
            SenderName = m.Sender.DisplayName,
            SenderAvatarUrl = m.Sender.AvatarUrl,
            FileName = m.Attachments.OrderBy(a => a.Id).Select(a => a.FileName).FirstOrDefault(),
            FileUrl = m.Attachments.OrderBy(a => a.Id).Select(a => a.FileUrl).FirstOrDefault(),
            FileContentType = m.Attachments.OrderBy(a => a.Id).Select(a => a.MimeType).FirstOrDefault()
        });
    }

    private IQueryable<GlobalSearchResultDto> BuildAttachmentQuery(int userId, GlobalSearchRequestDto request, string keyword)
    {
        var query = _db.MessageAttachments
            .AsNoTracking()
            .Where(a =>
                !a.Message.IsDeleted &&
                (
                    (a.Message.GroupId.HasValue && _db.GroupMembers.Any(gm =>
                        gm.GroupId == a.Message.GroupId.Value &&
                        gm.UserId == userId)) ||
                    (a.Message.GroupId == null &&
                        (a.Message.SenderId == userId || a.Message.ReceiverId == userId))
                ));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(a =>
                a.FileName.Contains(keyword) ||
                a.MimeType.Contains(keyword) ||
                (a.Message.Content != null && a.Message.Content.Contains(keyword)));
        }

        if (request.FromDate.HasValue)
            query = query.Where(a => a.Message.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(a => a.Message.CreatedAt <= request.ToDate.Value);

        if (request.SenderId.HasValue)
            query = query.Where(a => a.Message.SenderId == request.SenderId.Value);

        if (request.GroupId.HasValue)
            query = query.Where(a => a.Message.GroupId == request.GroupId.Value);

        if (request.PeerUserId.HasValue)
            query = query.Where(a =>
                a.Message.GroupId == null &&
                (
                    (a.Message.SenderId == userId && a.Message.ReceiverId == request.PeerUserId.Value) ||
                    (a.Message.SenderId == request.PeerUserId.Value && a.Message.ReceiverId == userId)
                ));

        return query.Select(a => new GlobalSearchResultDto
        {
            Type = "attachment",
            Id = a.Id,
            MessageId = a.MessageId,
            Title = a.FileName,
            Snippet = a.Message.Content == null
                ? a.MimeType
                : (a.Message.Content.Length > 180 ? a.Message.Content.Substring(0, 180) : a.Message.Content),
            CreatedAt = a.Message.CreatedAt,
            GroupId = a.Message.GroupId,
            PeerUserId = a.Message.GroupId == null
                ? (a.Message.SenderId == userId ? a.Message.ReceiverId : a.Message.SenderId)
                : null,
            GroupName = a.Message.Group != null ? a.Message.Group.Name : null,
            PeerName = a.Message.GroupId == null
                ? (a.Message.SenderId == userId
                    ? (a.Message.Receiver != null ? a.Message.Receiver.DisplayName : null)
                    : a.Message.Sender.DisplayName)
                : null,
            PeerAvatarUrl = a.Message.GroupId == null
                ? (a.Message.SenderId == userId
                    ? (a.Message.Receiver != null ? a.Message.Receiver.AvatarUrl : null)
                    : a.Message.Sender.AvatarUrl)
                : null,
            SenderName = a.Message.Sender.DisplayName,
            SenderAvatarUrl = a.Message.Sender.AvatarUrl,
            FileName = a.FileName,
            FileUrl = a.FileUrl,
            FileContentType = a.MimeType
        });
    }

    private IQueryable<GlobalSearchResultDto> BuildUserQuery(int userId, GlobalSearchRequestDto request, string keyword)
    {
        var query = _db.Users
            .AsNoTracking()
            .Where(u =>
                u.Id != userId &&
                !_db.BlockedUsers.Any(b =>
                    (b.BlockerUserId == userId && b.BlockedUserId == u.Id) ||
                    (b.BlockerUserId == u.Id && b.BlockedUserId == userId)));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(u =>
                u.DisplayName.Contains(keyword) ||
                u.Username.Contains(keyword) ||
                u.Email.Contains(keyword));
        }

        if (request.FromDate.HasValue)
            query = query.Where(u => u.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(u => u.CreatedAt <= request.ToDate.Value);

        return query.Select(u => new GlobalSearchResultDto
        {
            Type = "user",
            Id = u.Id,
            Title = u.DisplayName,
            Snippet = u.Username,
            CreatedAt = u.CreatedAt,
            AvatarUrl = u.AvatarUrl,
            PeerUserId = u.Id
        });
    }

    private IQueryable<GlobalSearchResultDto> BuildGroupQuery(int userId, GlobalSearchRequestDto request, string keyword)
    {
        var query = _db.Groups
            .AsNoTracking()
            .Where(g =>
                !g.IsDeleted &&
                _db.GroupMembers.Any(gm => gm.GroupId == g.Id && gm.UserId == userId));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(g =>
                g.Name.Contains(keyword) ||
                (g.Description != null && g.Description.Contains(keyword)));
        }

        if (request.FromDate.HasValue)
            query = query.Where(g => g.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(g => g.CreatedAt <= request.ToDate.Value);

        if (request.GroupId.HasValue)
            query = query.Where(g => g.Id == request.GroupId.Value);

        return query.Select(g => new GlobalSearchResultDto
        {
            Type = "group",
            Id = g.Id,
            Title = g.Name,
            Snippet = g.Description,
            CreatedAt = g.CreatedAt,
            AvatarUrl = g.AvatarUrl,
            GroupId = g.Id,
            GroupName = g.Name
        });
    }

    private IQueryable<GlobalSearchResultDto> BuildSnippetQuery(int userId, GlobalSearchRequestDto request, string keyword)
    {
        var query = _db.CodeSnippets
            .AsNoTracking()
            .Where(s => s.UserId == userId);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(s =>
                (s.Title != null && s.Title.Contains(keyword)) ||
                s.Language.Contains(keyword) ||
                s.Content.Contains(keyword));
        }

        if (request.FromDate.HasValue)
            query = query.Where(s => s.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(s => s.CreatedAt <= request.ToDate.Value);

        return query.Select(s => new GlobalSearchResultDto
        {
            Type = "snippet",
            Id = s.Id,
            Title = s.Title ?? s.Language,
            Snippet = s.Content.Length > 220 ? s.Content.Substring(0, 220) : s.Content,
            CreatedAt = s.CreatedAt,
            Url = null,
            Status = s.Language
        });
    }

    private IQueryable<GlobalSearchResultDto> BuildIocQuery(int userId, GlobalSearchRequestDto request, string keyword)
    {
        var query = _db.IocIndicators
            .AsNoTracking()
            .Where(i =>
                (i.GroupId.HasValue && _db.GroupMembers.Any(gm =>
                    gm.GroupId == i.GroupId.Value &&
                    gm.UserId == userId)) ||
                (!i.GroupId.HasValue &&
                    (i.CreatedByUserId == userId || i.PeerUserId == userId)));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(i =>
                i.Value.Contains(keyword) ||
                i.Type.Contains(keyword) ||
                i.Severity.Contains(keyword) ||
                i.Status.Contains(keyword) ||
                i.Source.Contains(keyword) ||
                (i.Description != null && i.Description.Contains(keyword)) ||
                (i.Tags != null && i.Tags.Contains(keyword)));
        }

        if (request.FromDate.HasValue)
            query = query.Where(i => i.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(i => i.CreatedAt <= request.ToDate.Value);

        if (request.GroupId.HasValue)
            query = query.Where(i => i.GroupId == request.GroupId.Value);

        if (request.PeerUserId.HasValue)
            query = query.Where(i => i.PeerUserId == request.PeerUserId.Value);

        if (!string.IsNullOrWhiteSpace(request.Severity))
        {
            var severity = NormalizeSeverity(request.Severity);
            query = query.Where(i => i.Severity == severity);
        }

        return query.Select(i => new GlobalSearchResultDto
        {
            Type = "ioc",
            Id = i.Id,
            Title = i.Value,
            Snippet = i.Description,
            CreatedAt = i.CreatedAt,
            MessageId = i.MessageId,
            GroupId = i.GroupId,
            PeerUserId = i.PeerUserId,
            GroupName = i.GroupId.HasValue
                ? _db.Groups
                    .Where(g => g.Id == i.GroupId.Value)
                    .Select(g => g.Name)
                    .FirstOrDefault()
                : null,
            PeerName = i.PeerUserId.HasValue
                ? _db.Users
                    .Where(u => u.Id == i.PeerUserId.Value)
                    .Select(u => u.DisplayName)
                    .FirstOrDefault()
                : null,
            PeerAvatarUrl = i.PeerUserId.HasValue
                ? _db.Users
                    .Where(u => u.Id == i.PeerUserId.Value)
                    .Select(u => u.AvatarUrl)
                    .FirstOrDefault()
                : null,
            Severity = i.Severity,
            Status = i.Status
        });
    }

    private IQueryable<GlobalSearchResultDto> BuildFindingQuery(int userId, GlobalSearchRequestDto request, string keyword)
    {
        var query = _db.PentestFindings
            .AsNoTracking()
            .Where(f => _db.GroupMembers.Any(gm =>
                gm.GroupId == f.GroupId &&
                gm.UserId == userId));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(f =>
                f.Title.Contains(keyword) ||
                f.Severity.Contains(keyword) ||
                f.Status.Contains(keyword) ||
                (f.Description != null && f.Description.Contains(keyword)) ||
                (f.PoC != null && f.PoC.Contains(keyword)) ||
                (f.AffectedTarget != null && f.AffectedTarget.Contains(keyword)) ||
                (f.AffectedEndpoint != null && f.AffectedEndpoint.Contains(keyword)) ||
                (f.Payload != null && f.Payload.Contains(keyword)) ||
                (f.Remediation != null && f.Remediation.Contains(keyword)));
        }

        if (request.FromDate.HasValue)
            query = query.Where(f => f.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(f => f.CreatedAt <= request.ToDate.Value);

        if (request.GroupId.HasValue)
            query = query.Where(f => f.GroupId == request.GroupId.Value);

        if (!string.IsNullOrWhiteSpace(request.Severity))
        {
            var severity = NormalizeSeverity(request.Severity);
            query = query.Where(f => f.Severity == severity);
        }

        return query.Select(f => new GlobalSearchResultDto
        {
            Type = "finding",
            Id = f.Id,
            Title = f.Title,
            Snippet = f.Payload != null
                ? (f.Payload.Length > 220 ? f.Payload.Substring(0, 220) : f.Payload)
                : (f.Description != null
                    ? (f.Description.Length > 220 ? f.Description.Substring(0, 220) : f.Description)
                    : (f.PoC != null ? (f.PoC.Length > 220 ? f.PoC.Substring(0, 220) : f.PoC) : null)),
            CreatedAt = f.CreatedAt,
            GroupId = f.GroupId,
            GroupName = f.Group.Name,
            Severity = f.Severity,
            Status = f.Status,
            AffectedEndpoint = f.AffectedEndpoint,
            HttpMethod = f.HttpMethod,
            PayloadPreview = f.Payload != null
                ? (f.Payload.Length > 160 ? f.Payload.Substring(0, 160) : f.Payload)
                : null,
            Url = $"/pentest?findingId={f.Id}"
        });
    }

    private IQueryable<GlobalSearchResultDto> BuildTaskQuery(int userId, GlobalSearchRequestDto request, string keyword)
    {
        var query = _db.GroupTasks
            .AsNoTracking()
            .Where(t => _db.GroupMembers.Any(gm =>
                gm.GroupId == t.GroupId &&
                gm.UserId == userId &&
                !gm.Group.IsDeleted));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(t =>
                t.Title.Contains(keyword) ||
                t.Priority.Contains(keyword) ||
                t.Status.Contains(keyword) ||
                (t.Description != null && t.Description.Contains(keyword)) ||
                (t.AssignedToUser != null && t.AssignedToUser.DisplayName.Contains(keyword)));
        }

        if (request.FromDate.HasValue)
            query = query.Where(t => t.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(t => t.CreatedAt <= request.ToDate.Value);

        if (request.GroupId.HasValue)
            query = query.Where(t => t.GroupId == request.GroupId.Value);

        if (request.PeerUserId.HasValue)
            query = query.Where(t => t.AssignedToUserId == request.PeerUserId.Value);

        if (!string.IsNullOrWhiteSpace(request.Severity))
        {
            var priority = NormalizeSeverity(request.Severity);
            query = query.Where(t => t.Priority == priority);
        }

        return query.Select(t => new GlobalSearchResultDto
        {
            Type = "task",
            Id = t.Id,
            Title = t.Title,
            Snippet = t.Description,
            CreatedAt = t.CreatedAt,
            MessageId = t.SourceMessageId,
            GroupId = t.GroupId,
            GroupName = t.Group.Name,
            PeerUserId = t.AssignedToUserId,
            PeerName = t.AssignedToUser != null ? t.AssignedToUser.DisplayName : null,
            Severity = t.Priority,
            Status = t.Status,
            Url = $"/tasks?taskId={t.Id}"
        });
    }

    private IQueryable<Message> BuildVisibleMessages(int userId)
    {
        return _db.Messages
            .AsNoTracking()
            .Where(m =>
                !m.IsDeleted &&
                (
                    (m.GroupId.HasValue && _db.GroupMembers.Any(gm =>
                        gm.GroupId == m.GroupId.Value &&
                        gm.UserId == userId &&
                        !gm.Group.IsDeleted)) ||       // ← lọc group đã xóa
                    (m.GroupId == null &&
                        (m.SenderId == userId || m.ReceiverId == userId))
                ));
    }

    private static IQueryable<Message> ApplyMessageFilters(
        IQueryable<Message> query,
        GlobalSearchRequestDto request,
        string keyword,
        bool includeAttachmentName)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(m =>
                (m.Content != null && m.Content.Contains(keyword)) ||
                m.Sender.DisplayName.Contains(keyword) ||
                (includeAttachmentName && m.Attachments.Any(a => a.FileName.Contains(keyword))));
        }

        if (request.FromDate.HasValue)
            query = query.Where(m => m.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(m => m.CreatedAt <= request.ToDate.Value);

        if (request.SenderId.HasValue)
            query = query.Where(m => m.SenderId == request.SenderId.Value);

        if (request.GroupId.HasValue)
            query = query.Where(m => m.GroupId == request.GroupId.Value);

        if (request.PeerUserId.HasValue)
        {
            query = query.Where(m =>
                m.GroupId == null &&
                (m.SenderId == request.PeerUserId.Value || m.ReceiverId == request.PeerUserId.Value));
        }

        return query;
    }

    private static string NormalizeType(string? type)
    {
        var normalized = (type ?? "all").Trim().ToLowerInvariant();
        return ValidTypes.Contains(normalized) ? normalized : "all";
    }

    private static bool ShouldSearch(string requestedType, string targetType)
        => requestedType == "all" || requestedType == targetType;

    private static string NormalizeSeverity(string? severity)
    {
        return (severity ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "info" => "Info",
            "low" => "Low",
            "medium" => "Medium",
            "high" => "High",
            "critical" => "Critical",
            _ => string.Empty
        };
    }
}
