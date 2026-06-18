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

        // Nguồn 1: Messages
        if (string.IsNullOrEmpty(query.EventType) || query.EventType == "message" || query.EventType == "file" || query.EventType == "finding" || query.EventType == "ioc" || query.EventType == "task")
        {
            var allowedMessageTypes = new[] 
            { 
                MessageType.Text, 
                MessageType.Image, 
                MessageType.File
            };

            var messagesQuery = _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Attachments)
                .AsNoTracking()
                .Where(m => m.GroupId == groupId && !m.IsDeleted && allowedMessageTypes.Contains(m.MessageType));

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

            var findingIds = new List<int>();
            var iocIds = new List<int>();

            foreach(var m in messages)
            {
                if (m.MessageType == MessageType.Vulnerability)
                {
                    try {
                        using var doc = System.Text.Json.JsonDocument.Parse(m.Content ?? "{}");
                        if (doc.RootElement.TryGetProperty("findingId", out var prop)) findingIds.Add(prop.GetInt32());
                    } catch {}
                }
                else if (m.MessageType == MessageType.Cve)
                {
                    try {
                        using var doc = System.Text.Json.JsonDocument.Parse(m.Content ?? "{}");
                        if (doc.RootElement.TryGetProperty("iocId", out var prop)) iocIds.Add(prop.GetInt32());
                    } catch {}
                }
            }

            var findingsDict = await _context.PentestFindings
                .Where(f => findingIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id);

            var iocDict = await _context.IocIndicators
                .Where(i => iocIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id);

            foreach (var m in messages)
            {
                var evtType = "message";
                if (m.MessageType == MessageType.Image || m.MessageType == MessageType.File) evtType = "file";
                else if (m.MessageType == MessageType.Vulnerability) evtType = "finding";
                else if (m.MessageType == MessageType.Cve) evtType = "ioc";
                else if (m.MessageType == MessageType.Task) evtType = "task";

                if (!string.IsNullOrEmpty(query.EventType) && query.EventType != evtType) continue;

                var preview = m.Content ?? "";
                var title = preview.Length > 100 ? preview.Substring(0, 100) + "..." : preview;
                string? severity = null;
                string? status = null;
                string? endpoint = null;
                string? iocType = null;
                string? priority = null;
                string? assignedTo = null;
                DateTime? dueDate = null;
                int sourceId = m.Id;

                if (evtType == "finding")
                {
                    int? fId = null;
                    try {
                        using var doc = System.Text.Json.JsonDocument.Parse(preview);
                        if (doc.RootElement.TryGetProperty("findingId", out var prop)) fId = prop.GetInt32();
                        if (doc.RootElement.TryGetProperty("severity", out var sevProp)) severity = sevProp.GetString();
                        if (doc.RootElement.TryGetProperty("status", out var statProp)) status = statProp.GetString();
                        if (doc.RootElement.TryGetProperty("title", out var titleProp)) title = titleProp.GetString() ?? title;
                    } catch {
                        if (preview.StartsWith("/vuln ", StringComparison.OrdinalIgnoreCase)) {
                            var parts = preview.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 3) {
                                severity = parts[1];
                                title = parts[2];
                            }
                        }
                    }

                    if (fId.HasValue && findingsDict.TryGetValue(fId.Value, out var f))
                    {
                        title = f.Title;
                        severity = f.Severity;
                        status = f.Status;
                        endpoint = f.AffectedEndpoint;
                        sourceId = f.Id;
                    }
                }
                else if (evtType == "ioc")
                {
                    int? iId = null;
                    try {
                        using var doc = System.Text.Json.JsonDocument.Parse(preview);
                        if (doc.RootElement.TryGetProperty("iocId", out var prop)) iId = prop.GetInt32();
                        if (doc.RootElement.TryGetProperty("value", out var valProp)) title = valProp.GetString() ?? title;
                        if (doc.RootElement.TryGetProperty("type", out var typeProp)) iocType = typeProp.GetString();
                    } catch {
                        if (preview.StartsWith("/ioc ", StringComparison.OrdinalIgnoreCase)) {
                            var parts = preview.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 3) {
                                iocType = parts[1];
                                title = parts[2];
                            }
                        }
                    }

                    if (iId.HasValue && iocDict.TryGetValue(iId.Value, out var ioc))
                    {
                        title = ioc.Value;
                        severity = ioc.Severity;
                        status = ioc.Status;
                        iocType = ioc.Type;
                        sourceId = ioc.Id;
                    }
                }
                else if (evtType == "task")
                {
                    try {
                        using var doc = System.Text.Json.JsonDocument.Parse(preview);
                        if (doc.RootElement.TryGetProperty("title", out var titleProp)) title = titleProp.GetString() ?? title;
                        if (doc.RootElement.TryGetProperty("priority", out var prioProp)) priority = prioProp.GetString();
                        if (doc.RootElement.TryGetProperty("status", out var statProp)) status = statProp.GetString();
                        if (doc.RootElement.TryGetProperty("assignedTo", out var assignProp)) assignedTo = assignProp.GetString();
                        if (doc.RootElement.TryGetProperty("dueDate", out var dueProp) && dueProp.ValueKind != System.Text.Json.JsonValueKind.Null) dueDate = dueProp.GetDateTime();
                    } catch {}
                }
                else if (evtType == "file")
                {
                     var attachment = m.Attachments.FirstOrDefault();
                     title = attachment != null ? attachment.FileName : title;
                }

                events.Add(new TimelineEventDto
                {
                    EventType = evtType,
                    Timestamp = m.CreatedAt,
                    ActorUserId = m.SenderId,
                    ActorDisplayName = m.Sender?.DisplayName ?? "",
                    ActorAvatar = m.Sender?.AvatarUrl,
                    SourceId = sourceId,
                    Title = title,
                    FileName = evtType == "file" && m.Attachments.Any() ? m.Attachments.First().FileName : null,
                    FileUrl = evtType == "file" && m.Attachments.Any() ? m.Attachments.First().FileUrl : null,
                    Severity = severity,
                    Status = status,
                    Endpoint = endpoint,
                    IocType = iocType,
                    Priority = priority,
                    AssignedTo = assignedTo,
                    DueDate = dueDate
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
                    Title = x.i.Value,
                    Severity = x.i.Severity,
                    Status = x.i.Status,
                    IocType = x.i.Type
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
                    Title = f.Title,
                    Severity = f.Severity,
                    Status = f.Status,
                    Endpoint = f.AffectedEndpoint
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
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.Priority,
                    AssignedTo = t.AssignedToUser?.DisplayName,
                    DueDate = t.DueAtUtc
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
