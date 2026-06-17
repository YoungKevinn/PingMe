using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs;
using PingMe.DTOs.Ioc;
using PingMe.DTOs.Message;
using PingMe.Hubs;
using PingMe.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PingMe.Services;

public interface IMessageService
{
    Task<List<ConversationAttachmentResponse>> GetConversationAttachmentsAsync(
    int userId,
    int? groupId,
    int? peerId,
    string type = "all",
    int limit = 80);
    Task<(bool Success, string? Error, MessageResponse? Message)> SendMessageAsync(int senderId, SendMessageRequest request);
    Task<(bool Success, string? Error, MessageResponse? Message)> UploadMessageAsync(
        int senderId,
        int? receiverId,
        int? groupId,
        int? replyToMessageId,
        IFormFile file,
        string backendBaseUrl,
        DateTime? expiresAt = null);
    Task<List<MessageResponse>> GetDmMessagesAsync(int userId, int peerId, int? before, int limit);
    Task<List<MessageResponse>> GetGroupMessagesAsync(int userId, int groupId, int? before, int limit);
    Task<List<MessageResponse>> GetMessageContextAsync(int userId, int messageId, int takeBefore = 20, int takeAfter = 20);
    Task<List<MessageResponse>> GetPinnedMessagesAsync(int? groupId, int? peerId, int userId);
    Task<(bool Success, string? Error)> EditMessageAsync(int messageId, int userId, string content);
    Task<(bool Success, string? Error)> DeleteMessageAsync(int messageId, int userId);
    Task<(bool Success, string? Error)> PinMessageAsync(int messageId, int userId, bool isPinned);
    Task<(bool Success, string? Error, MessageResponse? Message)> ForwardMessageAsync(int messageId, int senderId, int? receiverId, int? groupId);
    Task<List<EditHistoryDto>> GetEditHistoryAsync(int messageId, int userId);
    Task SendReminderDueMessageAsync(ReminderDto reminder);
}

public class MessageService : IMessageService
{
    private const long MaxUploadBytes = 25 * 1024 * 1024;
    private const int MaxTextMessageLength = 4000;
    private const int MaxPinnedMessagesPerConversation = 5;
    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".bat", ".cmd", ".com", ".msi", ".ps1", ".vbs", ".js", ".jar", ".php", ".sh"
    };

    private readonly AppDbContext _db;
    private readonly IHubContext<ChatHub> _hub;
    private readonly IBlockService _block;
    private readonly IWebHostEnvironment _env;
    private readonly IIocService _ioc;
    private readonly IPentestFindingService _findings;
    private readonly IReminderService _reminders;
    private readonly IGroupTaskService _tasks;

    public MessageService(
        AppDbContext db,
        IHubContext<ChatHub> hub,
        IBlockService block,
        IWebHostEnvironment env,
        IIocService ioc,
        IPentestFindingService findings,
        IReminderService reminders,
        IGroupTaskService tasks)
    {
        _db = db;
        _hub = hub;
        _block = block;
        _env = env;
        _ioc = ioc;
        _findings = findings;
        _reminders = reminders;
        _tasks = tasks;
    }
    public async Task<List<ConversationAttachmentResponse>> GetConversationAttachmentsAsync(
    int userId,
    int? groupId,
    int? peerId,
    string type = "all",
    int limit = 80)
    {
        limit = Math.Clamp(limit, 1, 200);
        type = string.IsNullOrWhiteSpace(type) ? "all" : type.Trim().ToLower();

        IQueryable<Message> query;

        if (groupId.HasValue)
        {
            var isMember = await _db.GroupMembers.AnyAsync(gm =>
                gm.GroupId == groupId.Value && gm.UserId == userId);

            if (!isMember)
                return [];

            query = _db.Messages.Where(m => m.GroupId == groupId.Value);
        }
        else if (peerId.HasValue)
        {
            query = _db.Messages.Where(m =>
                m.GroupId == null &&
                ((m.SenderId == userId && m.ReceiverId == peerId.Value) ||
                 (m.SenderId == peerId.Value && m.ReceiverId == userId)));
        }
        else
        {
            return [];
        }

        var attachmentsQuery = query
            .Where(m => !m.IsDeleted)
            .SelectMany(
                m => m.Attachments,
                (m, a) => new ConversationAttachmentResponse
                {
                    Id = a.Id,
                    MessageId = m.Id,
                    SenderId = m.SenderId,
                    SenderDisplayName = m.Sender.DisplayName,
                    GroupId = m.GroupId,
                    ReceiverId = m.ReceiverId,
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    FileSize = a.FileSize,
                    MimeType = a.MimeType,
                    IsImage = a.MimeType.StartsWith("image/"),
                    CreatedAt = a.CreatedAt
                });

        if (type == "images")
            attachmentsQuery = attachmentsQuery.Where(a => a.MimeType.StartsWith("image/"));
        else if (type == "files")
            attachmentsQuery = attachmentsQuery.Where(a => !a.MimeType.StartsWith("image/"));

        return await attachmentsQuery
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
    public async Task<(bool Success, string? Error, MessageResponse? Message)> SendMessageAsync(
        int senderId, SendMessageRequest request)
    {
        var validation = await ValidateConversationAsync(senderId, request.ReceiverId, request.GroupId);
        if (!validation.Success)
            return (false, validation.Error, null);

        if (!Enum.TryParse<MessageType>(request.MessageType, true, out var messageType))
            messageType = MessageType.Text;

        var normalizedContent = request.Content?.Trim();

        var contentValidation = ValidateMessageContent(normalizedContent, messageType);

        if (!contentValidation.Success)
            return (false, contentValidation.Error, null);

        var isIocCommand = IsIocCommand(normalizedContent);
        var isVulnCommand = PentestFindingService.IsVulnCommand(normalizedContent);
        var isReminderCommand = IsReminderCommand(normalizedContent);
        var isTaskCommand = IsTaskCommand(normalizedContent);

        if (isIocCommand)
        {
            var iocError = await _ioc.ValidateCommandCreateAsync(
                senderId,
                normalizedContent ?? string.Empty,
                request.GroupId,
                request.ReceiverId);

            if (!string.IsNullOrWhiteSpace(iocError))
                return (false, iocError, null);
        }

        if (isVulnCommand)
        {
            var vulnError = await _findings.ValidateCommandCreateAsync(
                senderId,
                normalizedContent ?? string.Empty,
                request.GroupId,
                request.ReceiverId);

            if (!string.IsNullOrWhiteSpace(vulnError))
                return (false, vulnError, null);

            return await SendVulnCommandMessageAsync(senderId, request, normalizedContent ?? string.Empty);
        }

        if (isReminderCommand)
            return await SendReminderCommandMessageAsync(senderId, request, normalizedContent ?? string.Empty);

        if (isTaskCommand)
            return await SendTaskCommandMessageAsync(senderId, request, normalizedContent ?? string.Empty);

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            GroupId = request.GroupId,
            Content = normalizedContent,
            MessageType = messageType,
            ReplyToMessageId = request.ReplyToMessageId,
            ForwardedFromMessageId = request.ForwardedFromMessageId,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        if (isIocCommand)
        {
            var (_, iocError) = await _ioc.CreateFromCommandAsync(senderId, new CreateIocFromCommandRequest
            {
                RawCommand = normalizedContent ?? string.Empty,
                MessageId = message.Id,
                PeerUserId = request.ReceiverId,
                GroupId = request.GroupId
            });

            if (!string.IsNullOrWhiteSpace(iocError))
                return (false, iocError, null);
        }

        var response = await BuildMessageResponseAsync(message.Id);

        // Detect mentions in group messages and notify mentioned users
        try
        {
            if (message.GroupId.HasValue && !string.IsNullOrWhiteSpace(response.Content))
            {
                var content = response.Content;
                var members = await _db.GroupMembers
                    .Where(gm => gm.GroupId == message.GroupId && gm.UserId != senderId)
                    .Include(gm => gm.User)
                    .ToListAsync();

                var notifyTasks = new List<Task>();

                foreach (var m in members)
                {
                    var display = m.User?.DisplayName;
                    if (string.IsNullOrWhiteSpace(display)) continue;

                    if (content.IndexOf("@" + display, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var payload = new
                        {
                            groupId = message.GroupId,
                            messageId = message.Id,
                            senderId = senderId,
                            senderDisplayName = response.SenderDisplayName,
                            mentionedUserId = m.UserId
                        };

                        notifyTasks.Add(_hub.Clients.Group($"user_{m.UserId}").SendAsync("UserMentioned", payload));
                    }
                }

                if (notifyTasks.Count > 0)
                    await Task.WhenAll(notifyTasks);
            }
        }
        catch
        {
            // Swallow mention notification errors so they don't break message sending
        }

        await BroadcastToConversationAsync(message, "ReceiveMessage", response);

        return (true, null, response);
    }

    private async Task<(bool Success, string? Error, MessageResponse? Message)> SendVulnCommandMessageAsync(
        int senderId,
        SendMessageRequest request,
        string rawCommand)
    {
        if (!request.GroupId.HasValue)
            return (false, "/vuln chỉ hỗ trợ group chat trong MVP.", null);

        var strategy = _db.Database.CreateExecutionStrategy();

        var result = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var findingResult = await _findings.CreateFromCommandAsync(senderId, rawCommand, request.GroupId.Value);
            if (findingResult.Finding is null)
            {
                await transaction.RollbackAsync();
                return (Success: false, Error: findingResult.Error, MessageId: (int?)null);
            }

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = null,
                GroupId = request.GroupId,
                Content = BuildVulnMessagePayload(findingResult.Finding),
                MessageType = MessageType.Vulnerability,
                ReplyToMessageId = request.ReplyToMessageId,
                ForwardedFromMessageId = request.ForwardedFromMessageId,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return (Success: true, Error: (string?)null, MessageId: (int?)message.Id);
        });

        if (!result.Success || !result.MessageId.HasValue)
            return (false, result.Error, null);

        var response = await BuildMessageResponseAsync(result.MessageId.Value);
        var savedMessage = await _db.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == result.MessageId.Value);

        if (savedMessage != null)
            await BroadcastToConversationAsync(savedMessage, "ReceiveMessage", response);

        return (true, null, response);
    }

    private async Task<(bool Success, string? Error, MessageResponse? Message)> SendReminderCommandMessageAsync(
        int senderId,
        SendMessageRequest request,
        string rawCommand)
    {
        var parsed = ParseReminderCommand(rawCommand);
        if (!parsed.Success)
            return (false, parsed.Error, null);

        var reminderResult = await _reminders.CreateAsync(senderId, new CreateReminderDto
        {
            Text = parsed.Text!,
            RemindAtUtc = parsed.RemindAtUtc!.Value,
            GroupId = request.GroupId,
            PeerUserId = request.ReceiverId
        });

        if (reminderResult.Reminder is null)
            return (false, reminderResult.Error, null);

        var reminder = reminderResult.Reminder;
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = request.ReceiverId,
            GroupId = request.GroupId,
            Content = BuildReminderMessagePayload(reminder, reminder.Status),
            MessageType = MessageType.Reminder,
            ReplyToMessageId = request.ReplyToMessageId,
            ForwardedFromMessageId = request.ForwardedFromMessageId,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var reminderEntity = await _db.ChatReminders.FirstOrDefaultAsync(r => r.Id == reminder.Id);
        if (reminderEntity is not null)
        {
            reminderEntity.SourceMessageId = message.Id;
            await _db.SaveChangesAsync();
        }

        var response = await BuildMessageResponseAsync(message.Id);
        await BroadcastToConversationAsync(message, "ReceiveMessage", response);

        return (true, null, response);
    }

    private async Task<(bool Success, string? Error, MessageResponse? Message)> SendTaskCommandMessageAsync(
        int senderId,
        SendMessageRequest request,
        string rawCommand)
    {
        var taskError = await _tasks.ValidateCommandCreateAsync(
            senderId,
            rawCommand,
            request.GroupId,
            request.ReceiverId);

        if (!string.IsNullOrWhiteSpace(taskError))
            return (false, taskError, null);

        if (!request.GroupId.HasValue)
            return (false, "Cần chọn group để tạo task từ /task.", null);

        var strategy = _db.Database.CreateExecutionStrategy();

        var result = await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var taskResult = await _tasks.CreateFromCommandAsync(senderId, rawCommand, request.GroupId.Value);
            if (taskResult.Task is null)
            {
                await transaction.RollbackAsync();
                return (Success: false, Error: taskResult.Error, MessageId: (int?)null);
            }

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = null,
                GroupId = request.GroupId,
                Content = BuildTaskMessagePayload(taskResult.Task),
                MessageType = MessageType.Task,
                ReplyToMessageId = request.ReplyToMessageId,
                ForwardedFromMessageId = request.ForwardedFromMessageId,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();
            await _tasks.AttachSourceMessageAsync(taskResult.Task.Id, message.Id);
            await transaction.CommitAsync();

            return (Success: true, Error: (string?)null, MessageId: (int?)message.Id);
        });

        if (!result.Success || !result.MessageId.HasValue)
            return (false, result.Error, null);

        var response = await BuildMessageResponseAsync(result.MessageId.Value);
        var savedMessage = await _db.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == result.MessageId.Value);

        if (savedMessage != null)
            await BroadcastToConversationAsync(savedMessage, "ReceiveMessage", response);

        return (true, null, response);
    }

    public async Task<(bool Success, string? Error, MessageResponse? Message)> UploadMessageAsync(
        int senderId,
        int? receiverId,
        int? groupId,
        int? replyToMessageId,
        IFormFile file,
        string backendBaseUrl,
        DateTime? expiresAt = null)
    {
        var validation = await ValidateConversationAsync(senderId, receiverId, groupId);
        if (!validation.Success)
            return (false, validation.Error, null);

        if (file is null || file.Length <= 0)
            return (false, "File không hợp lệ hoặc đang rỗng.", null);

        if (file.Length > MaxUploadBytes)
            return (false, "File vượt quá dung lượng cho phép 25MB.", null);

        var originalFileName = Path.GetFileName(file.FileName);
        if (string.IsNullOrWhiteSpace(originalFileName))
            originalFileName = "attachment";

        var extension = Path.GetExtension(originalFileName);
        if (BlockedExtensions.Contains(extension))
            return (false, "Loại file này đang bị chặn để đảm bảo an toàn.", null);

        var safeFileName = MakeSafeFileName(originalFileName);
        var dateFolder = DateTime.UtcNow.ToString("yyyyMMdd");
        var storedFileName = $"{Guid.NewGuid():N}_{safeFileName}";

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

        var uploadDir = Path.Combine(webRoot, "uploads", "messages", dateFolder);
        Directory.CreateDirectory(uploadDir);

        var physicalPath = Path.Combine(uploadDir, storedFileName);
        await using (var stream = new FileStream(physicalPath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/messages/{dateFolder}/{storedFileName}";
        var fileUrl = CombineBaseUrl(backendBaseUrl, relativeUrl);
        var mimeType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        var messageType = mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            ? MessageType.Image
            : mimeType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
                ? MessageType.Audio
                : MessageType.File;

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            GroupId = groupId,
            Content = originalFileName,
            MessageType = messageType,
            ReplyToMessageId = replyToMessageId,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        _db.MessageAttachments.Add(new MessageAttachment
        {
            MessageId = message.Id,
            FileName = originalFileName,
            FileUrl = fileUrl,
            FileSize = file.Length,
            MimeType = mimeType,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var response = await BuildMessageResponseAsync(message.Id);
        await BroadcastToConversationAsync(message, "ReceiveMessage", response);

        return (true, null, response);
    }

    public async Task<List<MessageResponse>> GetDmMessagesAsync(int userId, int peerId, int? before, int limit)
    {
        limit = Math.Clamp(limit, 1, 50);

        var query = _db.Messages
            .AsNoTracking()
            .Where(m => m.GroupId == null)
            .Where(m => (m.SenderId == userId && m.ReceiverId == peerId) ||
                        (m.SenderId == peerId && m.ReceiverId == userId));

        if (before.HasValue)
        {
            var cursor = await query
                .Where(m => m.Id == before.Value)
                .Select(m => new { m.Id, m.CreatedAt })
                .FirstOrDefaultAsync();

            if (cursor is not null)
            {
                query = query.Where(m =>
                    m.CreatedAt < cursor.CreatedAt ||
                    (m.CreatedAt == cursor.CreatedAt && m.Id < cursor.Id));
            }
        }

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(limit)
            .Select(m => new MessageResponse
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderDisplayName = m.Sender.DisplayName,
                SenderAvatarUrl = m.Sender.AvatarUrl,
                GroupId = m.GroupId,
                ReceiverId = m.ReceiverId,
                Content = m.Content,
                MessageType = m.MessageType.ToString(),
                ReplyToMessageId = m.ReplyToMessageId,
                ReplyToMessage = m.ReplyToMessage == null ? null : new MessageResponse
                {
                    Id = m.ReplyToMessage.Id,
                    SenderId = m.ReplyToMessage.SenderId,
                    SenderDisplayName = m.ReplyToMessage.Sender.DisplayName,
                    Content = m.ReplyToMessage.IsDeleted ? "Tin nhắn đã thu hồi" : m.ReplyToMessage.Content,
                    CreatedAt = m.ReplyToMessage.CreatedAt
                },
                ForwardedFromMessageId = m.ForwardedFromMessageId,
                IsDeleted = m.IsDeleted,
                IsEdited = m.IsEdited,
                IsPinned = m.IsPinned,
                ExpiresAt = m.ExpiresAt,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
                Attachments = m.Attachments
                    .OrderBy(a => a.Id)
                    .Select(a => new AttachmentResponse
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FileUrl = a.FileUrl,
                        FileSize = a.FileSize,
                        MimeType = a.MimeType
                    })
                    .ToList(),
                Reactions = m.Reactions
                    .GroupBy(r => r.Emoji)
                    .Select(g => new ReactionSummary
                    {
                        Emoji = g.Key,
                        Count = g.Count(),
                        UserIds = g.Select(r => r.UserId).ToList()
                    })
                    .ToList(),
                ReadByUserIds = m.ReadReceipts.Select(r => r.UserId).ToList()
            })
            .ToListAsync();

        messages.Reverse();
        return messages;
    }

    public async Task<List<MessageResponse>> GetGroupMessagesAsync(int userId, int groupId, int? before, int limit)
    {
        limit = Math.Clamp(limit, 1, 50);

        var member = await _db.GroupMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId && gm.UserId == userId);
        if (member == null)
            throw new UnauthorizedAccessException("Bạn không còn là thành viên của nhóm này.");

        var query = _db.Messages
            .AsNoTracking()
            .Where(m => m.GroupId == groupId);

        if (member.ClearedAt.HasValue)
        {
            query = query.Where(m => m.CreatedAt > member.ClearedAt.Value);
        }

        if (before.HasValue)
        {
            var cursor = await query
                .Where(m => m.Id == before.Value)
                .Select(m => new { m.Id, m.CreatedAt })
                .FirstOrDefaultAsync();

            if (cursor is not null)
            {
                query = query.Where(m =>
                    m.CreatedAt < cursor.CreatedAt ||
                    (m.CreatedAt == cursor.CreatedAt && m.Id < cursor.Id));
            }
        }

        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Take(limit)
            .Select(m => new MessageResponse
            {
                Id = m.Id,
                SenderId = m.SenderId,
                SenderDisplayName = m.Sender.DisplayName,
                SenderAvatarUrl = m.Sender.AvatarUrl,
                GroupId = m.GroupId,
                ReceiverId = m.ReceiverId,
                Content = m.Content,
                MessageType = m.MessageType.ToString(),
                ReplyToMessageId = m.ReplyToMessageId,
                ReplyToMessage = m.ReplyToMessage == null ? null : new MessageResponse
                {
                    Id = m.ReplyToMessage.Id,
                    SenderId = m.ReplyToMessage.SenderId,
                    SenderDisplayName = m.ReplyToMessage.Sender.DisplayName,
                    Content = m.ReplyToMessage.IsDeleted ? "Tin nhắn đã thu hồi" : m.ReplyToMessage.Content,
                    CreatedAt = m.ReplyToMessage.CreatedAt
                },
                ForwardedFromMessageId = m.ForwardedFromMessageId,
                IsDeleted = m.IsDeleted,
                IsEdited = m.IsEdited,
                IsPinned = m.IsPinned,
                ExpiresAt = m.ExpiresAt,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
                Attachments = m.Attachments
                    .OrderBy(a => a.Id)
                    .Select(a => new AttachmentResponse
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FileUrl = a.FileUrl,
                        FileSize = a.FileSize,
                        MimeType = a.MimeType
                    })
                    .ToList(),
                Reactions = m.Reactions
                    .GroupBy(r => r.Emoji)
                    .Select(g => new ReactionSummary
                    {
                        Emoji = g.Key,
                        Count = g.Count(),
                        UserIds = g.Select(r => r.UserId).ToList()
                    })
                    .ToList(),
                ReadByUserIds = m.ReadReceipts.Select(r => r.UserId).ToList()
            })
            .ToListAsync();

        messages.Reverse();
        return messages;
    }

    public async Task<List<MessageResponse>> GetMessageContextAsync(int userId, int messageId, int takeBefore = 20, int takeAfter = 20)
    {
        takeBefore = Math.Clamp(takeBefore, 1, 50);
        takeAfter = Math.Clamp(takeAfter, 1, 50);

        var target = await _db.Messages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);
        if (target is null)
            return [];

        if (!await CanAccessMessageAsync(userId, target))
            return [];

        IQueryable<Message> conversationQuery = BuildConversationQuery(target).AsNoTracking();

        var before = await conversationQuery
            .Where(m => m.Id < messageId)
            .OrderByDescending(m => m.Id)
            .Take(takeBefore)
            .IncludeMessageGraph()
            .ToListAsync();

        before.Reverse();

        var center = await conversationQuery
            .Where(m => m.Id == messageId)
            .IncludeMessageGraph()
            .ToListAsync();

        var after = await conversationQuery
            .Where(m => m.Id > messageId)
            .OrderBy(m => m.Id)
            .Take(takeAfter)
            .IncludeMessageGraph()
            .ToListAsync();

        return before.Concat(center).Concat(after)
            .OrderBy(m => m.Id)
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<List<MessageResponse>> GetPinnedMessagesAsync(int? groupId, int? peerId, int userId)
    {
        if (!groupId.HasValue && !peerId.HasValue)
            return [];

        var query = _db.Messages.AsNoTracking().Where(m => m.IsPinned && !m.IsDeleted);

        if (groupId.HasValue)
        {
            var isMember = await _db.GroupMembers
                .AsNoTracking()
                .AnyAsync(gm => gm.GroupId == groupId.Value && gm.UserId == userId);

            if (!isMember)
                return [];

            query = query.Where(m => m.GroupId == groupId);
        }
        else if (peerId.HasValue)
        {
            query = query.Where(m =>
                (m.SenderId == userId && m.ReceiverId == peerId) ||
                (m.SenderId == peerId && m.ReceiverId == userId));
        }

        var messages = await query
            .OrderByDescending(m => m.UpdatedAt)
            .AsSplitQuery()
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .Include(m => m.Reactions).ThenInclude(r => r.User)
            .Include(m => m.ReadReceipts)
            .Include(m => m.ReplyToMessage).ThenInclude(r => r!.Sender)
            .ToListAsync();

        return messages.Select(MapToResponse).ToList();
    }

    public async Task<(bool Success, string? Error)> EditMessageAsync(int messageId, int userId, string content)
    {
        var message = await _db.Messages.FindAsync(messageId);
        if (message is null || message.IsDeleted) return (false, "Tin nhắn không tồn tại.");
        if (message.SenderId != userId) return (false, "Không có quyền chỉnh sửa.");

        // Lưu lịch sử chỉnh sửa
        var normalizedContent = content?.Trim() ?? string.Empty;

        var contentValidation = ValidateMessageContent(normalizedContent, MessageType.Text);

        if (!contentValidation.Success)
            return (false, contentValidation.Error);

        // Lưu lịch sử chỉnh sửa
        _db.MessageEditHistories.Add(new MessageEditHistory
        {
            MessageId = messageId,
            OldContent = message.Content ?? string.Empty,
            NewContent = normalizedContent,
            EditedAt = DateTime.UtcNow
        });

        message.Content = normalizedContent;
        message.IsEdited = true;
        message.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var response = await BuildMessageResponseAsync(messageId);
        await BroadcastToConversationAsync(message, "MessageEdited", response);

        return (true, null);
}

    public async Task<(bool Success, string? Error)> DeleteMessageAsync(int messageId, int userId)
    {
        var message = await _db.Messages.FindAsync(messageId);
        if (message is null) return (false, "Tin nhắn không tồn tại.");

        bool isAdmin = false;
        if (message.GroupId.HasValue)
            isAdmin = await _db.GroupMembers.AnyAsync(gm =>
                gm.GroupId == message.GroupId && gm.UserId == userId &&
                (gm.Role == GroupMemberRole.Admin || gm.Role == GroupMemberRole.CoAdmin));

        if (message.SenderId != userId && !isAdmin)
            return (false, "Không có quyền xóa.");

        message.IsDeleted = true;
        message.Content = "Tin nhắn đã thu hồi";
        message.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await BroadcastToConversationAsync(message, "MessageDeleted",
            new { messageId, message.GroupId, message.ReceiverId });

        return (true, null);
    }

    private static bool IsIocCommand(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        return content.TrimStart().StartsWith("/ioc", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReminderCommand(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        return content.TrimStart().StartsWith("/reminder", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTaskCommand(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        return content.TrimStart().StartsWith("/task", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<(bool Success, string? Error)> PinMessageAsync(int messageId, int userId, bool isPinned)
    {
        var message = await _db.Messages.FindAsync(messageId);
        if (message is null) return (false, "Tin nhắn không tồn tại.");
        if (message.IsDeleted) return (false, "Tin nhắn đã bị thu hồi.");

        if (message.GroupId.HasValue)
        {
            var ok = await _db.GroupMembers.AnyAsync(gm =>
                gm.GroupId == message.GroupId && gm.UserId == userId &&
                (gm.Role == GroupMemberRole.Admin || gm.Role == GroupMemberRole.CoAdmin));
            if (!ok) return (false, "Chỉ admin/co-admin mới được ghim tin nhắn.");
        }
        else
        {
            // DM: chỉ cho phép 2 người trong hội thoại pin/unpin
            if (message.SenderId != userId && message.ReceiverId != userId)
                return (false, "Không có quyền ghim tin nhắn này.");
        }

        if (isPinned && !message.IsPinned)
        {
            var currentPinnedCount = await BuildConversationQuery(message)
                .Where(m =>
                    m.Id != messageId &&
                    m.IsPinned &&
                    !m.IsDeleted)
                .CountAsync();

            if (currentPinnedCount >= MaxPinnedMessagesPerConversation)
            {
                return (false, $"Mỗi hội thoại chỉ được ghim tối đa {MaxPinnedMessagesPerConversation} tin nhắn.");
            }
        }

        message.IsPinned = isPinned;
        message.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Broadcast realtime cho mọi client trong hội thoại
        var response = await BuildMessageResponseAsync(messageId);
        await BroadcastToConversationAsync(message, "MessagePinned", new
        {
            messageId,
            isPinned,
            groupId = message.GroupId,
            receiverId = message.ReceiverId,
            senderId = message.SenderId,
            message = response
        });
        await AddSystemNotificationAsync(message, userId, isPinned ? "đã ghim một tin nhắn" : "đã bỏ ghim một tin nhắn");

        return (true, null);
    }

    public async Task<(bool Success, string? Error, MessageResponse? Message)> ForwardMessageAsync(
        int messageId, int senderId, int? receiverId, int? groupId)
    {
        var original = await _db.Messages.FindAsync(messageId);
        if (original is null || original.IsDeleted) return (false, "Tin nhắn không tồn tại.", null);

        return await SendMessageAsync(senderId, new SendMessageRequest
        {
            ReceiverId = receiverId,
            GroupId = groupId,
            Content = original.Content,
            MessageType = original.MessageType.ToString(),
            ForwardedFromMessageId = messageId
        });
    }

    public async Task SendReminderDueMessageAsync(ReminderDto reminder)
    {
        if (reminder.GroupId is null && reminder.PeerUserId is null)
            return;

        if (reminder.GroupId.HasValue)
        {
            var isMember = await _db.GroupMembers
                .AsNoTracking()
                .AnyAsync(gm => gm.GroupId == reminder.GroupId.Value && gm.UserId == reminder.CreatedByUserId && !gm.Group.IsDeleted);

            if (!isMember)
                return;
        }

        var message = new Message
        {
            SenderId = reminder.CreatedByUserId,
            ReceiverId = reminder.GroupId.HasValue ? null : reminder.PeerUserId,
            GroupId = reminder.GroupId,
            Content = BuildReminderMessagePayload(reminder, ReminderService.Sent),
            MessageType = MessageType.Reminder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        var response = await BuildMessageResponseAsync(message.Id);
        await BroadcastToConversationAsync(message, "ReceiveMessage", response);
    }

    private static (bool Success, string? Error) ValidateMessageContent(string? content, MessageType messageType)
    {
        var requiresContent = messageType is not MessageType.File and not MessageType.Image;

        if (requiresContent && string.IsNullOrWhiteSpace(content))
            return (false, "Nội dung tin nhắn không được để trống.");

        if ((content?.Length ?? 0) > MaxTextMessageLength)
        {
            return (false, $"Nội dung tin nhắn tối đa {MaxTextMessageLength} ký tự. Vui lòng rút ngắn nội dung hoặc gửi dưới dạng file/snippet.");
        }

        return (true, null);
    }
    private async Task<MessageResponse> BuildMessageResponseAsync(int messageId)
    {
        var m = await _db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .Include(m => m.Reactions).ThenInclude(r => r.User)
            .Include(m => m.ReadReceipts)
            .Include(m => m.ReplyToMessage).ThenInclude(r => r!.Sender)
            .FirstAsync(m => m.Id == messageId);

        return MapToResponse(m);
    }

    private static string BuildVulnMessagePayload(PentestFindingDetailDto finding)
    {
        return JsonSerializer.Serialize(new
        {
            type = "vulnerability",
            findingId = finding.Id,
            groupId = finding.GroupId,
            severity = finding.Severity,
            status = finding.Status,
            title = finding.Title,
            affectedTarget = finding.AffectedTarget,
            affectedEndpoint = finding.AffectedEndpoint,
            httpMethod = finding.HttpMethod,
            payload = finding.Payload,
            url = $"/pentest?findingId={finding.Id}"
        });
    }

    private static string BuildReminderMessagePayload(ReminderDto reminder, string status)
    {
        return JsonSerializer.Serialize(new
        {
            type = "reminder",
            reminderId = reminder.Id,
            text = reminder.Text,
            remindAtUtc = reminder.RemindAtUtc,
            status,
            groupId = reminder.GroupId,
            peerUserId = reminder.PeerUserId,
            sourceMessageId = reminder.SourceMessageId
        });
    }

    private static string BuildTaskMessagePayload(GroupTaskResponseDto task)
    {
        return JsonSerializer.Serialize(new
        {
            type = "task",
            taskId = task.Id,
            groupId = task.GroupId,
            title = task.Title,
            priority = task.Priority,
            status = task.Status,
            dueAtUtc = task.DueAtUtc,
            assignedToUserId = task.AssignedToUserId,
            assignedToDisplayName = task.AssignedToDisplayName,
            url = $"/tasks?taskId={task.Id}"
        });
    }

    private static (bool Success, string? Error, DateTime? RemindAtUtc, string? Text) ParseReminderCommand(string rawCommand)
    {
        var match = Regex.Match(
            rawCommand.Trim(),
            @"^/reminder\s+(?<amount>\d+)\s*(?<unit>[a-zA-Z])\s+(?<text>[\s\S]+)$",
            RegexOptions.IgnoreCase);

        if (!match.Success)
            return (false, "Cú pháp: /reminder 10p nội dung nhắc", null, null);

        if (!int.TryParse(match.Groups["amount"].Value, out var amount))
            return (false, "Cú pháp: /reminder 10p nội dung nhắc", null, null);

        var unit = match.Groups["unit"].Value.ToLowerInvariant();
        var text = match.Groups["text"].Value.Trim();

        if (amount <= 0)
            return (false, "Thời gian nhắc phải lớn hơn 0.", null, null);

        if (string.IsNullOrWhiteSpace(text))
            return (false, "Cú pháp: /reminder 10p nội dung nhắc", null, null);

        if (text.Length > 500)
            return (false, "Nội dung nhắc việc tối đa 500 ký tự.", null, null);

        var totalDays = unit switch
        {
            "p" or "m" => amount / 1440d,
            "h" => amount / 24d,
            "d" or "n" => amount,
            _ => -1d
        };

        if (totalDays < 0)
            return (false, "Thời gian hỗ trợ: p/m = phút, h = giờ, d/n = ngày", null, null);

        if (totalDays > 365)
            return (false, "Thời gian nhắc tối đa 365 ngày.", null, null);

        var offset = unit switch
        {
            "p" or "m" => TimeSpan.FromMinutes(amount),
            "h" => TimeSpan.FromHours(amount),
            "d" or "n" => TimeSpan.FromDays(amount),
            _ => TimeSpan.Zero
        };

        return (true, null, DateTime.UtcNow.Add(offset), text);
    }

    private async Task<(bool Success, string? Error)> ValidateConversationAsync(int senderId, int? receiverId, int? groupId)
    {
        if (receiverId is null && groupId is null)
            return (false, "Phải có ReceiverId hoặc GroupId.");

        if (receiverId.HasValue && groupId.HasValue)
            return (false, "Chỉ được gửi vào DM hoặc Group, không được chọn cả hai.");

        if (receiverId.HasValue)
        {
            if (receiverId.Value == senderId)
                return (false, "Không thể tự gửi tin nhắn cho chính mình.");

            if (await _block.IsBlockedAsync(senderId, receiverId.Value))
                return (false, "Không thể gửi tin nhắn cho người này.");
        }

        if (groupId.HasValue)
        {
            var isMember = await _db.GroupMembers.AnyAsync(gm =>
                gm.GroupId == groupId.Value && gm.UserId == senderId);
            if (!isMember)
                return (false, "Bạn không còn là thành viên của nhóm này.");
        }

        return (true, null);
    }

    private async Task<bool> CanAccessMessageAsync(int userId, Message message)
    {
        if (message.GroupId.HasValue)
        {
            return await _db.GroupMembers.AnyAsync(gm =>
                gm.GroupId == message.GroupId.Value && gm.UserId == userId);
        }

        return message.SenderId == userId || message.ReceiverId == userId;
    }

    private IQueryable<Message> BuildConversationQuery(Message target)
    {
        if (target.GroupId.HasValue)
            return _db.Messages.Where(m => m.GroupId == target.GroupId.Value);

        return _db.Messages.Where(m =>
            m.GroupId == null &&
            ((m.SenderId == target.SenderId && m.ReceiverId == target.ReceiverId) ||
             (m.SenderId == target.ReceiverId && m.ReceiverId == target.SenderId)));
    }

    private static MessageResponse MapToResponse(Message m) => new()
    {
        Id = m.Id,
        SenderId = m.SenderId,
        SenderDisplayName = m.Sender.DisplayName,
        SenderAvatarUrl = m.Sender.AvatarUrl,
        GroupId = m.GroupId,
        ReceiverId = m.ReceiverId,
        Content = m.Content,
        MessageType = m.MessageType.ToString(),
        ReplyToMessageId = m.ReplyToMessageId,
        ReplyToMessage = m.ReplyToMessage is null ? null : new MessageResponse
        {
            Id = m.ReplyToMessage.Id,
            SenderId = m.ReplyToMessage.SenderId,
            SenderDisplayName = m.ReplyToMessage.Sender?.DisplayName ?? string.Empty,
            Content = m.ReplyToMessage.IsDeleted ? "Tin nhắn đã thu hồi" : m.ReplyToMessage.Content,
            CreatedAt = m.ReplyToMessage.CreatedAt
        },
        ForwardedFromMessageId = m.ForwardedFromMessageId,
        IsDeleted = m.IsDeleted,
        IsEdited = m.IsEdited,
        IsPinned = m.IsPinned,
        ExpiresAt = m.ExpiresAt,
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt,
        Attachments = m.Attachments.Select(a => new AttachmentResponse
        {
            Id = a.Id,
            FileName = a.FileName,
            FileUrl = a.FileUrl,
            FileSize = a.FileSize,
            MimeType = a.MimeType
        }).ToList(),
        Reactions = m.Reactions
            .GroupBy(r => r.Emoji)
            .Select(g => new ReactionSummary
            {
                Emoji = g.Key,
                Count = g.Count(),
                UserIds = g.Select(r => r.UserId).ToList()
            }).ToList(),
        ReadByUserIds = m.ReadReceipts.Select(r => r.UserId).ToList()
    };

    private async Task BroadcastToConversationAsync(Message message, string eventName, object payload)
    {
        if (message.GroupId.HasValue)
        {
            await _hub.Clients.Group($"group_{message.GroupId}").SendAsync(eventName, payload);
        }
        else
        {
            await _hub.Clients.Group($"user_{message.SenderId}").SendAsync(eventName, payload);
            if (message.ReceiverId.HasValue)
                await _hub.Clients.Group($"user_{message.ReceiverId}").SendAsync(eventName, payload);
        }
    }

    private async Task AddSystemNotificationAsync(Message conversationMessage, int actorId, string action)
    {
        var actorName = await _db.Users
            .Where(u => u.Id == actorId)
            .Select(u => u.DisplayName)
            .FirstOrDefaultAsync() ?? "Người dùng";

        var systemMessage = new Message
        {
            SenderId = actorId,
            ReceiverId = conversationMessage.GroupId.HasValue ? null : GetDmPeerId(conversationMessage, actorId),
            GroupId = conversationMessage.GroupId,
            Content = $"{actorName} {action}",
            MessageType = MessageType.System,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Messages.Add(systemMessage);
        await _db.SaveChangesAsync();

        var response = await BuildMessageResponseAsync(systemMessage.Id);
        await BroadcastToConversationAsync(systemMessage, "ReceiveMessage", response);
    }

    private static int? GetDmPeerId(Message message, int actorId)
    {
        if (message.SenderId == actorId)
            return message.ReceiverId;

        return message.SenderId;
    }

    private static string MakeSafeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        name = Regex.Replace(name, @"[^a-zA-Z0-9_\.\-\(\)\[\] ]", "_");
        name = Regex.Replace(name, @"\s+", "_");
        return string.IsNullOrWhiteSpace(name) ? "attachment" : name;
    }

    private static string CombineBaseUrl(string baseUrl, string relativeUrl)
    {
        return $"{baseUrl.TrimEnd('/')}/{relativeUrl.TrimStart('/')}";
    }

    public async Task<List<EditHistoryDto>> GetEditHistoryAsync(int messageId, int userId)
    {
        var message = await _db.Messages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message is null || !await CanAccessMessageAsync(userId, message))
            return [];

        return await _db.MessageEditHistories
            .Where(h => h.MessageId == messageId)
            .OrderByDescending(h => h.EditedAt)
            .Select(h => new EditHistoryDto
            {
                Id = h.Id,
                OldContent = h.OldContent,
                NewContent = h.NewContent,
                EditedAt = h.EditedAt
            })
            .ToListAsync();
    }
}

internal static class MessageQueryableExtensions
{
    public static IQueryable<Message> IncludeMessageGraph(this IQueryable<Message> query)
    {
        return query
            .AsSplitQuery()
            .Include(m => m.Sender)
            .Include(m => m.Attachments)
            .Include(m => m.Reactions).ThenInclude(r => r.User)
            .Include(m => m.ReadReceipts)
            .Include(m => m.ReplyToMessage).ThenInclude(r => r!.Sender);
    }
}
