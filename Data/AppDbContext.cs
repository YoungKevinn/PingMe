using Microsoft.EntityFrameworkCore;
using PingMe.Models;
using System.Reflection.Emit;

namespace PingMe.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Core chat ──
    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<MessageAttachment> MessageAttachments => Set<MessageAttachment>();
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();
    public DbSet<SavedMessage> SavedMessages => Set<SavedMessage>();
    public DbSet<MessageReadReceipt> MessageReadReceipts => Set<MessageReadReceipt>();
    public DbSet<MessageEditHistory> MessageEditHistories => Set<MessageEditHistory>();
    // ── Security & features ──
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<BlockedUser> BlockedUsers => Set<BlockedUser>();
    public DbSet<PinnedConversation> PinnedConversations => Set<PinnedConversation>();
    public DbSet<ConversationNickname> ConversationNicknames => Set<ConversationNickname>();
    public DbSet<ConversationBackground> ConversationBackgrounds => Set<ConversationBackground>();
    public DbSet<CodeSnippet> CodeSnippets => Set<CodeSnippet>();
    public DbSet<SnippetAccessLog> SnippetAccessLogs => Set<SnippetAccessLog>();
    public DbSet<Webhook> Webhooks => Set<Webhook>();
    public DbSet<IocIndicator> IocIndicators => Set<IocIndicator>();
    public DbSet<PentestFinding> PentestFindings => Set<PentestFinding>();
    public DbSet<ChatReminder> ChatReminders => Set<ChatReminder>();
    public DbSet<GroupTask> GroupTasks => Set<GroupTask>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<OneTimeSecret> OneTimeSecrets => Set<OneTimeSecret>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ─────────────────────────────────────────────────
        // USER
        // ─────────────────────────────────────────────────
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();

            e.Property(u => u.IsOnline).HasDefaultValue(false);
            e.Property(u => u.CreatedAt).HasDefaultValueSql("NOW(6)");
            e.Property(u => u.UpdatedAt).HasDefaultValueSql("NOW(6)");
        });

        // FRIEND REQUEST
        b.Entity<FriendRequest>(e =>
        {
            e.HasIndex(f => new { f.FromUserId, f.ToUserId }).IsUnique();
            e.Property(f => f.CreatedAt).HasDefaultValueSql("NOW(6)");
        });

        // FRIENDSHIP
        b.Entity<Friendship>(e =>
        {
            e.HasIndex(f => new { f.UserAId, f.UserBId }).IsUnique();
            e.Property(f => f.CreatedAt).HasDefaultValueSql("NOW(6)");
        });

        // ─────────────────────────────────────────────────
        // GROUP
        // ─────────────────────────────────────────────────
        b.Entity<Group>(e =>
        {
            e.Property(g => g.IsDeleted).HasDefaultValue(false);
            e.Property(g => g.CreatedAt).HasDefaultValueSql("NOW(6)");
            e.Property(g => g.UpdatedAt).HasDefaultValueSql("NOW(6)");

            // Creator → RESTRICT (không xóa User nếu còn group do họ tạo)
            e.HasOne(g => g.CreatedBy)
             .WithMany()
             .HasForeignKey(g => g.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ─────────────────────────────────────────────────
        // GROUP MEMBER
        // ─────────────────────────────────────────────────
        b.Entity<GroupMember>(e =>
        {
            e.HasIndex(gm => new { gm.GroupId, gm.UserId }).IsUnique();

            e.Property(gm => gm.Role)
             .HasConversion<string>()
             .HasDefaultValue(GroupMemberRole.Member);

            e.Property(gm => gm.JoinedAt).HasDefaultValueSql("NOW(6)");

            e.HasOne(gm => gm.Group)
             .WithMany(g => g.Members)
             .HasForeignKey(gm => gm.GroupId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(gm => gm.User)
             .WithMany(u => u.GroupMemberships)
             .HasForeignKey(gm => gm.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─────────────────────────────────────────────────
        // MESSAGE
        // ─────────────────────────────────────────────────
        b.Entity<Message>(e =>
        {
            e.Property(m => m.MessageType)
             .HasConversion<string>()
             .HasDefaultValue(MessageType.Text);

            e.Property(m => m.IsDeleted).HasDefaultValue(false);
            e.Property(m => m.IsEdited).HasDefaultValue(false);
            e.Property(m => m.IsPinned).HasDefaultValue(false);
            e.Property(m => m.CreatedAt).HasDefaultValueSql("NOW(6)");
            e.Property(m => m.UpdatedAt).HasDefaultValueSql("NOW(6)");

            // Sender → RESTRICT (bảo toàn lịch sử)
            e.HasOne(m => m.Sender)
             .WithMany(u => u.SentMessages)
             .HasForeignKey(m => m.SenderId)
             .OnDelete(DeleteBehavior.Restrict);

            // Receiver (DM) → nullable, SET NULL khi user bị xóa
            e.HasOne(m => m.Receiver)
             .WithMany(u => u.ReceivedMessages)
             .HasForeignKey(m => m.ReceiverId)
             .OnDelete(DeleteBehavior.SetNull);

            // Group → CASCADE
            e.HasOne(m => m.Group)
             .WithMany(g => g.Messages)
             .HasForeignKey(m => m.GroupId)
             .OnDelete(DeleteBehavior.Cascade);

            // Self-reference: Reply
            e.HasOne(m => m.ReplyToMessage)
             .WithMany()
             .HasForeignKey(m => m.ReplyToMessageId)
             .OnDelete(DeleteBehavior.SetNull);

            // Self-reference: Forward
            e.HasOne(m => m.ForwardedFromMessage)
             .WithMany()
             .HasForeignKey(m => m.ForwardedFromMessageId)
             .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            e.HasIndex(m => m.SenderId);
            e.HasIndex(m => m.GroupId);
            e.HasIndex(m => m.ReceiverId);
            e.HasIndex(m => m.CreatedAt);
            e.HasIndex(m => m.ExpiresAt); // background job query
        });
        // ─────────────────────────────────────────────────
        // IOC INDICATOR
        // ─────────────────────────────────────────────────
        b.Entity<IocIndicator>(e =>
        {
            e.ToTable("IocIndicators");

            e.HasKey(x => x.Id);

            e.Property(x => x.Type)
                .HasMaxLength(32)
                .IsRequired();

            e.Property(x => x.Value)
                .HasMaxLength(2048)
                .IsRequired();

            e.Property(x => x.Description)
                .HasMaxLength(4000);

            e.Property(x => x.Severity)
                .HasMaxLength(32)
                .IsRequired();

            e.Property(x => x.Status)
                .HasMaxLength(32)
                .IsRequired();

            e.Property(x => x.Source)
                .HasMaxLength(64)
                .IsRequired();

            e.Property(x => x.Tags)
                .HasMaxLength(512);

            e.Property(x => x.CreatedAt)
                .HasDefaultValueSql("NOW(6)");

            e.Property(x => x.UpdatedAt)
                .HasDefaultValueSql("NOW(6)");

            e.HasIndex(x => x.Type);
            e.HasIndex(x => x.Severity);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.GroupId);
            e.HasIndex(x => x.PeerUserId);
            e.HasIndex(x => x.MessageId);
            e.HasIndex(x => x.CreatedByUserId);
        });
        // ─────────────────────────────────────────────────
        // PENTEST FINDING
        // ─────────────────────────────────────────────────
        b.Entity<PentestFinding>(e =>
        {
            e.ToTable("PentestFindings");

            e.HasKey(x => x.Id);

            e.Property(x => x.Title)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(x => x.Severity)
                .HasMaxLength(32)
                .IsRequired();

            e.Property(x => x.Status)
                .HasMaxLength(32)
                .IsRequired();

            e.Property(x => x.Description)
                .HasMaxLength(4000);

            e.Property(x => x.PoC)
                .HasMaxLength(4000);

            e.Property(x => x.Remediation)
                .HasMaxLength(4000);

            e.Property(x => x.AffectedTarget)
                .HasMaxLength(300);

            e.Property(x => x.AffectedEndpoint)
                .HasMaxLength(500);

            e.Property(x => x.HttpMethod)
                .HasMaxLength(16);

            e.Property(x => x.CreatedAt)
                .HasDefaultValueSql("NOW(6)");

            e.HasOne(x => x.Group)
                .WithMany()
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.GroupId);
            e.HasIndex(x => x.Severity);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.CreatedByUserId);
            e.HasIndex(x => x.CreatedAt);
        });
        // ─────────────────────────────────────────────────
        // CHAT REMINDER
        // ─────────────────────────────────────────────────
        b.Entity<ChatReminder>(e =>
        {
            e.ToTable("ChatReminders");

            e.HasKey(x => x.Id);

            e.Property(x => x.Text)
                .HasMaxLength(500)
                .IsRequired();

            e.Property(x => x.Status)
                .HasMaxLength(30)
                .IsRequired();

            e.Property(x => x.CreatedAt)
                .HasDefaultValueSql("NOW(6)");

            e.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Group)
                .WithMany()
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.PeerUser)
                .WithMany()
                .HasForeignKey(x => x.PeerUserId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.SourceMessage)
                .WithMany()
                .HasForeignKey(x => x.SourceMessageId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.CreatedByUserId);
            e.HasIndex(x => x.GroupId);
            e.HasIndex(x => x.PeerUserId);
            e.HasIndex(x => x.RemindAtUtc);
        });
        // ─────────────────────────────────────────────────
        // GROUP TASK
        // ─────────────────────────────────────────────────
        b.Entity<GroupTask>(e =>
        {
            e.ToTable("GroupTasks");

            e.HasKey(x => x.Id);

            e.Property(x => x.Title)
                .HasMaxLength(200)
                .IsRequired();

            e.Property(x => x.Description)
                .HasMaxLength(1000);

            e.Property(x => x.Priority)
                .HasMaxLength(20)
                .IsRequired();

            e.Property(x => x.Status)
                .HasMaxLength(30)
                .IsRequired();

            e.Property(x => x.CreatedAt)
                .HasDefaultValueSql("NOW(6)");

            e.Property(x => x.UpdatedAt)
                .HasDefaultValueSql("NOW(6)");

            e.HasOne(x => x.Group)
                .WithMany(g => g.Tasks)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.CreatedByUser)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.AssignedToUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(x => x.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.SourceMessage)
                .WithMany()
                .HasForeignKey(x => x.SourceMessageId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(x => x.GroupId);
            e.HasIndex(x => x.CreatedByUserId);
            e.HasIndex(x => x.AssignedToUserId);
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Priority);
            e.HasIndex(x => x.DueAtUtc);
            e.HasIndex(x => x.SourceMessageId);
        });
        // ─────────────────────────────────────────────────
        // MESSAGE ATTACHMENT
        // ─────────────────────────────────────────────────
        b.Entity<MessageAttachment>(e =>
        {
            e.Property(a => a.CreatedAt).HasDefaultValueSql("NOW(6)");

            e.HasOne(a => a.Message)
             .WithMany(m => m.Attachments)
             .HasForeignKey(a => a.MessageId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─────────────────────────────────────────────────
        // MESSAGE REACTION
        // ─────────────────────────────────────────────────
        b.Entity<MessageReaction>(e =>
        {
            e.HasIndex(r => new { r.MessageId, r.UserId, r.Emoji }).IsUnique();

            e.Property(r => r.CreatedAt).HasDefaultValueSql("NOW(6)");

            e.HasOne(r => r.Message)
             .WithMany(m => m.Reactions)
             .HasForeignKey(r => r.MessageId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.User)
             .WithMany(u => u.Reactions)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─────────────────────────────────────────────────
        // SAVED MESSAGE
        // ─────────────────────────────────────────────────
        b.Entity<SavedMessage>(e =>
        {
            e.HasIndex(s => new { s.UserId, s.MessageId }).IsUnique();
            e.HasIndex(s => new { s.UserId, s.CreatedAt });

            e.Property(s => s.CreatedAt).HasDefaultValueSql("NOW(6)");

            e.HasOne(s => s.User)
             .WithMany(u => u.SavedMessages)
             .HasForeignKey(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.Message)
             .WithMany(m => m.SavedMessages)
             .HasForeignKey(s => s.MessageId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─────────────────────────────────────────────────
        // MESSAGE READ RECEIPT
        // ─────────────────────────────────────────────────
        b.Entity<MessageReadReceipt>(e =>
        {
            e.HasIndex(r => new { r.MessageId, r.UserId }).IsUnique();

            e.Property(r => r.ReadAt).HasDefaultValueSql("NOW(6)");

            e.HasOne(r => r.Message)
             .WithMany(m => m.ReadReceipts)
             .HasForeignKey(r => r.MessageId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.User)
             .WithMany(u => u.ReadReceipts)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─────────────────────────────────────────────────
        // USER SESSION
        // ─────────────────────────────────────────────────
        b.Entity<UserSession>(e =>
        {
            e.Property(s => s.IsRevoked).HasDefaultValue(false);
            e.Property(s => s.CreatedAt).HasDefaultValueSql("NOW(6)");
            e.Property(s => s.LastActive).HasDefaultValueSql("NOW(6)");

            e.HasIndex(s => s.TokenHash);
            e.HasIndex(s => s.UserId);
            e.HasIndex(s => s.ExpiresAt);

            e.HasOne(s => s.User)
             .WithMany(u => u.Sessions)
             .HasForeignKey(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ─────────────────────────────────────────────────
        // AUDIT LOG
        // ─────────────────────────────────────────────────
        b.Entity<AuditLog>(e =>
        {
            e.Property(a => a.CreatedAt).HasDefaultValueSql("NOW(6)");

            e.HasIndex(a => a.UserId);
            e.HasIndex(a => a.CreatedAt);
            e.HasIndex(a => a.Action);

            // SET NULL khi user bị xóa — vẫn giữ audit log
            e.HasOne(a => a.User)
             .WithMany(u => u.AuditLogs)
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ─────────────────────────────────────────────────
        // BLOCKED USER
        // ─────────────────────────────────────────────────
        b.Entity<BlockedUser>(e =>
        {
            e.HasIndex(b2 => new { b2.BlockerUserId, b2.BlockedUserId }).IsUnique();

            e.Property(b2 => b2.CreatedAt).HasDefaultValueSql("NOW(6)");

            e.HasOne(b2 => b2.Blocker)
             .WithMany(u => u.BlockedUsers)
             .HasForeignKey(b2 => b2.BlockerUserId)
             .OnDelete(DeleteBehavior.Cascade);

            // Dùng NoAction để tránh multiple cascade paths trong MySQL
            e.HasOne(b2 => b2.Blocked)
             .WithMany(u => u.BlockedByUsers)
             .HasForeignKey(b2 => b2.BlockedUserId)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // ─────────────────────────────────────────────────
        // PINNED CONVERSATION
        // ─────────────────────────────────────────────────
        b.Entity<PinnedConversation>(e =>
        {
            e.Property(p => p.PinnedAt).HasDefaultValueSql("NOW(6)");

            e.HasOne(p => p.User)
             .WithMany(u => u.PinnedConversations)
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.PeerUser)
             .WithMany()
             .HasForeignKey(p => p.PeerUserId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(p => p.Group)
             .WithMany()
             .HasForeignKey(p => p.GroupId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(p => new { p.UserId, p.PeerUserId, p.GroupId });
        });

        // ─────────────────────────────────────────────────
        // CONVERSATION NICKNAME
        // ─────────────────────────────────────────────────
        b.Entity<ConversationNickname>(e =>
        {
            e.HasIndex(n => new { n.SetByUserId, n.TargetUserId, n.GroupId }).IsUnique();

            e.Property(n => n.CreatedAt).HasDefaultValueSql("NOW(6)");
            e.Property(n => n.UpdatedAt).HasDefaultValueSql("NOW(6)");

            e.HasOne(n => n.SetBy)
             .WithMany(u => u.NicknamesSet)
             .HasForeignKey(n => n.SetByUserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(n => n.Target)
             .WithMany(u => u.NicknamesReceived)
             .HasForeignKey(n => n.TargetUserId)
             .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(n => n.Group)
             .WithMany()
             .HasForeignKey(n => n.GroupId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ─────────────────────────────────────────────────
        // CONVERSATION BACKGROUND
        // ─────────────────────────────────────────────────
        b.Entity<ConversationBackground>(e =>
        {
            e.Property(bg => bg.BackgroundType)
             .HasConversion<string>();

            e.Property(bg => bg.CreatedAt).HasDefaultValueSql("NOW(6)");
            e.Property(bg => bg.UpdatedAt).HasDefaultValueSql("NOW(6)");

            e.HasOne(bg => bg.User)
             .WithMany(u => u.ConversationBackgrounds)
             .HasForeignKey(bg => bg.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(bg => bg.PeerUser)
             .WithMany()
             .HasForeignKey(bg => bg.PeerUserId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(bg => bg.Group)
             .WithMany()
             .HasForeignKey(bg => bg.GroupId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(bg => new { bg.UserId, bg.PeerUserId, bg.GroupId });
        });

        // ─────────────────────────────────────────────────
        // CODE SNIPPET
        // ─────────────────────────────────────────────────
        b.Entity<CodeSnippet>(e =>
        {
            e.HasIndex(s => s.ShareToken).IsUnique();
            e.HasIndex(s => s.ExpiresAt);

            e.Property(s => s.IsRevoked).HasDefaultValue(false);
            e.Property(s => s.AccessCount).HasDefaultValue(0);
            e.Property(s => s.CreatedAt).HasDefaultValueSql("NOW(6)");
            e.Property(s => s.UpdatedAt).HasDefaultValueSql("NOW(6)");

            e.HasOne(s => s.User)
             .WithMany(u => u.CodeSnippets)
             .HasForeignKey(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.Message)
             .WithMany(m => m.CodeSnippets)
             .HasForeignKey(s => s.MessageId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ─────────────────────────────────────────────────
        // SNIPPET ACCESS LOG
        // ─────────────────────────────────────────────────
        b.Entity<SnippetAccessLog>(e =>
        {
            e.HasIndex(l => l.SnippetId);
            e.HasIndex(l => l.AccessedAt);
            e.Property(l => l.AccessedAt).HasDefaultValueSql("NOW(6)");

            e.HasOne(l => l.Snippet)
             .WithMany(s => s.AccessLogs)
             .HasForeignKey(l => l.SnippetId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(l => l.User)
             .WithMany()
             .HasForeignKey(l => l.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ─────────────────────────────────────────────────
        // MESSAGE → SNIPPET FK (for Snippet-type messages)
        // ─────────────────────────────────────────────────
        b.Entity<Message>(e =>
        {
            e.HasOne(m => m.Snippet)
             .WithMany()
             .HasForeignKey(m => m.SnippetId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ─────────────────────────────────────────────────
        // WEBHOOK
        // ─────────────────────────────────────────────────
        b.Entity<Webhook>(e =>
        {
            e.HasIndex(w => w.Token).IsUnique();

            e.Property(w => w.IsActive).HasDefaultValue(true);
            e.Property(w => w.CreatedAt).HasDefaultValueSql("NOW(6)");
            e.Property(w => w.UpdatedAt).HasDefaultValueSql("NOW(6)");

            e.HasOne(w => w.Group)
             .WithMany(g => g.Webhooks)
             .HasForeignKey(w => w.GroupId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(w => w.CreatedBy)
             .WithMany()
             .HasForeignKey(w => w.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<MessageEditHistory>(e =>
        {
            e.HasIndex(h => h.MessageId);
            e.Property(h => h.EditedAt).HasDefaultValueSql("NOW(6)");
        });

        // ─────────────────────────────────────────────────
        // ONE-TIME SECRET
        // ─────────────────────────────────────────────────
        b.Entity<OneTimeSecret>(e =>
        {
            e.HasIndex(s => s.TokenHash).IsUnique();
            e.HasIndex(s => new { s.CreatedByUserId, s.CreatedAt });
            e.HasIndex(s => s.ExpiresAt);
            e.HasIndex(s => s.ViewedByUserId);

            e.Property(s => s.TokenHash)
             .HasMaxLength(64)
             .IsRequired();

            e.Property(s => s.SecretCipherText)
             .IsRequired();

            e.Property(s => s.ViewedIpHash)
             .HasMaxLength(64);

            e.Property(s => s.ViewedUserAgent)
             .HasMaxLength(512);

            e.Property(s => s.IsViewed).HasDefaultValue(false);
            e.Property(s => s.IsRevoked).HasDefaultValue(false);
            e.Property(s => s.CreatedAt).HasDefaultValueSql("NOW(6)");

            e.HasOne(s => s.CreatedByUser)
             .WithMany(u => u.OneTimeSecrets)
             .HasForeignKey(s => s.CreatedByUserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(s => s.ViewedByUser)
             .WithMany()
             .HasForeignKey(s => s.ViewedByUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });
        // ─────────────────────────────────────────────────
        // PASSWORD RESET OTP
        // ─────────────────────────────────────────────────
        b.Entity<PasswordResetOtp>(e => {
            e.Property(x => x.OtpCode).IsRequired().HasMaxLength(6);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.OtpCode);
        });
    }
}
