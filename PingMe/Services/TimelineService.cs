using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs;
using PingMe.Models;

namespace PingMe.Services;

public class TimelineService
{
    private readonly AppDbContext _context;

    public TimelineService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TimelineEventDto>> GetGroupTimelineAsync(int currentUserId, int groupId, TimelineQueryDto query)
    {
        // Bước 1: Verify user là GroupMember của groupId
        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == currentUserId);
        
        if (member == null)
        {
            throw new UnauthorizedAccessException("Bạn không phải là thành viên của nhóm này.");
        }

        var events = new List<TimelineEventDto>();

        // Nguồn 1: File/Evidence từ message attachment. Không đưa chat text thường vào Threat Timeline.
        if (string.IsNullOrEmpty(query.EventType) || query.EventType == "file")
        {
            var messagesQuery = _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Attachments)
                .AsNoTracking()
                .Where(m =>
                    m.GroupId == groupId &&
                    !m.IsDeleted &&
                    (m.MessageType == MessageType.Image || m.MessageType == MessageType.File) &&
                    m.Attachments.Any());

            if (query.From.HasValue) messagesQuery = messagesQuery.Where(m => m.CreatedAt >= query.From.Value);
            if (query.To.HasValue) messagesQuery = messagesQuery.Where(m => m.CreatedAt <= query.To.Value);
            
            if (member.ClearedAt.HasValue) 
                messagesQuery = messagesQuery.Where(m => m.CreatedAt >= member.ClearedAt.Value);
                
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var search = query.SearchTerm.ToLower();
                messagesQuery = messagesQuery.Where(m => 
                    (m.Content != null && m.Content.ToLower().Contains(search)) ||
                    (m.Attachments.Any(a => a.FileName.ToLower().Contains(search)))
                );
            }

            var messages = await messagesQuery.ToListAsync();

            foreach (var m in messages)
            {
                var attachment = m.Attachments.FirstOrDefault();
                if (attachment == null)
                    continue;

                events.Add(new TimelineEventDto
                {
                    EventType = "file",
                    Timestamp = m.CreatedAt,
                    ActorUserId = m.SenderId,
                    ActorDisplayName = m.Sender?.DisplayName ?? "",
                    ActorAvatar = m.Sender?.AvatarUrl,
                    SourceId = m.Id,
                    RelatedId = attachment.Id,
                    MessageId = m.Id,
                    GroupId = groupId,
                    Title = attachment.FileName,
                    FileName = attachment.FileName,
                    FileUrl = attachment.FileUrl,
                    ActionText = "Xem File",
                    ActionUrl = attachment.FileUrl
                });
            }
        }

        // Nguồn 2: IocIndicators
        if (string.IsNullOrEmpty(query.EventType) || query.EventType == "ioc")
        {
            var iocQuery = _context.IocIndicators
                .Join(_context.Users, i => i.CreatedByUserId, u => u.Id, (i, u) => new { i, u })
                .AsNoTracking()
                .Where(x => x.i.GroupId == groupId);

            if (query.From.HasValue) iocQuery = iocQuery.Where(x => x.i.CreatedAt >= query.From.Value);
            if (query.To.HasValue) iocQuery = iocQuery.Where(x => x.i.CreatedAt <= query.To.Value);
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var search = query.SearchTerm.ToLower();
                iocQuery = iocQuery.Where(x => 
                    (x.i.Value != null && x.i.Value.ToLower().Contains(search)) ||
                    (x.i.Type != null && x.i.Type.ToLower().Contains(search)));
            }

            var iocs = await iocQuery.ToListAsync();
            foreach (var x in iocs)
            {
                events.Add(new TimelineEventDto
                {
                    EventType = "ioc",
                    Timestamp = x.i.CreatedAt,
                    ActorUserId = x.i.CreatedByUserId,
                    ActorDisplayName = x.u.DisplayName,
                    ActorAvatar = x.u.AvatarUrl,
                    SourceId = x.i.Id,
                    RelatedId = x.i.Id,
                    MessageId = x.i.MessageId,
                    GroupId = x.i.GroupId,
                    Title = x.i.IsDeleted ? $"{x.i.Value} (Đã xóa)" : x.i.Value,
                    Severity = x.i.Severity,
                    Status = x.i.Status,
                    IocType = x.i.Type,
                    ActionText = "Xem IOC",
                    ActionUrl = x.i.IsDeleted ? null : $"/ioc?highlightId={x.i.Id}"
                });
            }
        }

        // Nguồn 3: PentestFindings
        if (string.IsNullOrEmpty(query.EventType) || query.EventType == "finding")
        {
            var findingQuery = _context.PentestFindings
                .Include(f => f.CreatedByUser)
                .AsNoTracking()
                .Where(f => f.GroupId == groupId);

            if (query.From.HasValue) findingQuery = findingQuery.Where(f => f.CreatedAt >= query.From.Value);
            if (query.To.HasValue) findingQuery = findingQuery.Where(f => f.CreatedAt <= query.To.Value);
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var search = query.SearchTerm.ToLower();
                findingQuery = findingQuery.Where(f => 
                    (f.Title != null && f.Title.ToLower().Contains(search)) ||
                    (f.AffectedEndpoint != null && f.AffectedEndpoint.ToLower().Contains(search)));
            }

            var findings = await findingQuery.ToListAsync();
            foreach (var f in findings)
            {
                events.Add(new TimelineEventDto
                {
                    EventType = "finding",
                    Timestamp = f.CreatedAt,
                    ActorUserId = f.CreatedByUserId,
                    ActorDisplayName = f.CreatedByUser?.DisplayName ?? "",
                    ActorAvatar = f.CreatedByUser?.AvatarUrl,
                    SourceId = f.Id,
                    RelatedId = f.Id,
                    GroupId = f.GroupId,
                    Title = f.IsDeleted ? $"{f.Title} (Đã xóa)" : f.Title,
                    Severity = f.Severity,
                    Status = f.Status,
                    Endpoint = f.AffectedEndpoint,
                    ActionText = "Xem Finding",
                    ActionUrl = f.IsDeleted ? null : $"/pentest?findingId={f.Id}"
                });
            }
        }

        // Nguồn 4: GroupTasks
        if (string.IsNullOrEmpty(query.EventType) || query.EventType == "task")
        {
            var taskQuery = _context.GroupTasks
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedToUser)
                .AsNoTracking()
                .Where(t => t.GroupId == groupId);

            if (query.From.HasValue) taskQuery = taskQuery.Where(t => t.CreatedAt >= query.From.Value);
            if (query.To.HasValue) taskQuery = taskQuery.Where(t => t.CreatedAt <= query.To.Value);
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var search = query.SearchTerm.ToLower();
                taskQuery = taskQuery.Where(t => 
                    (t.Title != null && t.Title.ToLower().Contains(search)));
            }

            var tasks = await taskQuery.ToListAsync();
            foreach (var t in tasks)
            {
                events.Add(new TimelineEventDto
                {
                    EventType = "task",
                    Timestamp = t.CreatedAt,
                    ActorUserId = t.CreatedByUserId,
                    ActorDisplayName = t.CreatedByUser?.DisplayName ?? "",
                    ActorAvatar = t.CreatedByUser?.AvatarUrl,
                    SourceId = t.Id,
                    RelatedId = t.Id,
                    MessageId = t.SourceMessageId,
                    GroupId = t.GroupId,
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.Priority,
                    AssignedTo = t.AssignedToUser?.DisplayName,
                    DueDate = t.DueAtUtc,
                    ActionText = "Xem Task",
                    ActionUrl = $"/tasks?taskId={t.Id}"
                });
            }
        }

        // Merge, sort, paginate
        var sortedEvents = events
            .OrderByDescending(e => e.Timestamp)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return sortedEvents;
    }
}
