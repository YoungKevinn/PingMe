using Microsoft.EntityFrameworkCore;
using PingMe.Data;
using PingMe.DTOs.Ioc;
using PingMe.Models;
using System.Text.RegularExpressions;

namespace PingMe.Services;

public interface IIocService
{
    Task<(IocResponse? Ioc, string? Error)> CreateAsync(int userId, CreateIocRequest request);

    Task<string?> ValidateCommandCreateAsync(
        int userId,
        string rawCommand,
        int? groupId,
        int? peerUserId);

    Task<(IocResponse? Ioc, string? Error)> CreateFromCommandAsync(
        int userId,
        CreateIocFromCommandRequest request);

    Task<List<IocResponse>> SearchAsync(int userId, IocSearchRequest request);

    Task<IocStatsResponse> GetStatsAsync(int userId);

    Task<IocResponse?> GetByIdAsync(int userId, int id);

    Task<(IocResponse? Ioc, string? Error)> UpdateAsync(int userId, int id, UpdateIocRequest request);

    Task<(bool Success, string? Error)> DeleteAsync(int userId, int id);

    Task<(IocResponse? Ioc, string? Error)> UpdateStatusAsync(int userId, int id, string status);
}

public class IocService : IIocService
{
    private readonly AppDbContext _db;

    private static readonly Regex IocCommandRegex = new(
        @"^\s*/ioc\s+(?<type>cve|ip|ipv4|md5|sha256|hash|url|domain)\s+(?<rest>[\s\S]+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex IocCommandMissingValueRegex = new(
        @"^\s*/ioc\s+(?<type>cve|ip|ipv4|md5|sha256|hash|url|domain)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CveRegex = new(
        @"^(?:CVE-)?(?<year>\d{4})-(?<id>\d{4,7})$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Ipv4Regex = new(
        @"^(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)$",
        RegexOptions.Compiled);

    private static readonly Regex Md5Regex = new(
        @"^[a-fA-F0-9]{32}$",
        RegexOptions.Compiled);

    private static readonly Regex Sha256Regex = new(
        @"^[a-fA-F0-9]{64}$",
        RegexOptions.Compiled);

    private static readonly Regex DomainRegex = new(
        @"^(?!-)(?:[a-zA-Z0-9-]{1,63}\.)+[a-zA-Z]{2,63}$",
        RegexOptions.Compiled);

    public IocService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IocResponse? Ioc, string? Error)> CreateAsync(int userId, CreateIocRequest request)
    {
        var normalized = NormalizeAndValidate(request.Type, request.Value);

        if (!normalized.Valid)
            return (null, normalized.Error);

        var severity = NormalizeSeverity(request.Severity);
        var status = NormalizeStatus(request.Status);

        var scopeValidation = await ValidateCreateScopeAsync(userId, request.GroupId, request.PeerUserId);
        if (!scopeValidation.Success)
            return (null, scopeValidation.Error);

        var duplicateQuery = _db.IocIndicators.Where(i =>
            i.Type == normalized.Type &&
            i.Value == normalized.Value &&
            i.GroupId == request.GroupId &&
            i.PeerUserId == request.PeerUserId);

        if (!request.GroupId.HasValue && !request.PeerUserId.HasValue)
        {
            duplicateQuery = duplicateQuery.Where(i => i.CreatedByUserId == userId);
        }

        if (await duplicateQuery.AnyAsync())
            return (null, "IOC n�y d� t?n t?i trong ph?m vi hi?n t?i.");

        var ioc = new IocIndicator
        {
            Type = normalized.Type,
            Value = normalized.Value,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Severity = severity,
            Status = status,
            Source = string.IsNullOrWhiteSpace(request.Source) ? "Manual" : request.Source.Trim(),
            Tags = string.IsNullOrWhiteSpace(request.Tags) ? null : request.Tags.Trim(),
            CreatedByUserId = userId,
            MessageId = request.MessageId,
            PeerUserId = request.PeerUserId,
            GroupId = request.GroupId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ResolvedAt = IsResolvedStatus(status) ? DateTime.UtcNow : null
        };

        _db.IocIndicators.Add(ioc);
        await _db.SaveChangesAsync();

        return (await MapAsync(ioc), null);
    }

    public async Task<(IocResponse? Ioc, string? Error)> CreateFromCommandAsync(
        int userId,
        CreateIocFromCommandRequest request)
    {
        var parsed = ParseCommand(request.RawCommand);

        if (!parsed.Success)
            return (null, parsed.Error);

        var createRequest = new CreateIocRequest
        {
            Type = parsed.Type,
            Value = parsed.Value,
            Description = parsed.Description,
            Severity = parsed.Severity,
            Status = "Open",
            Source = "ChatCommand",
            MessageId = request.MessageId,
            PeerUserId = request.PeerUserId,
            GroupId = request.GroupId
        };

        return await CreateAsync(userId, createRequest);
    }

    public async Task<string?> ValidateCommandCreateAsync(
        int userId,
        string rawCommand,
        int? groupId,
        int? peerUserId)
    {
        var parsed = ParseCommand(rawCommand);

        if (!parsed.Success)
            return parsed.Error;

        var normalized = NormalizeAndValidate(parsed.Type, parsed.Value);

        if (!normalized.Valid)
            return normalized.Error;

        var scopeValidation = await ValidateCreateScopeAsync(userId, groupId, peerUserId);

        if (!scopeValidation.Success)
            return scopeValidation.Error;

        var duplicateQuery = _db.IocIndicators.Where(i =>
            i.Type == normalized.Type &&
            i.Value == normalized.Value &&
            i.GroupId == groupId &&
            i.PeerUserId == peerUserId);

        if (!groupId.HasValue && !peerUserId.HasValue)
            duplicateQuery = duplicateQuery.Where(i => i.CreatedByUserId == userId);

        if (await duplicateQuery.AnyAsync())
            return "IOC n�y d� t?n t?i trong ph?m vi hi?n t?i.";

        return null;
    }

    public async Task<List<IocResponse>> SearchAsync(int userId, IocSearchRequest request)
    {
        var query = VisibleIocs(userId);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();

            query = query.Where(i =>
                i.Value.Contains(keyword) ||
                (i.Description != null && i.Description.Contains(keyword)) ||
                (i.Tags != null && i.Tags.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = NormalizeType(request.Type);
            query = query.Where(i => i.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(request.Severity))
        {
            var severity = NormalizeSeverity(request.Severity);
            query = query.Where(i => i.Severity == severity);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = NormalizeStatus(request.Status);
            query = query.Where(i => i.Status == status);
        }

        if (request.GroupId.HasValue)
            query = query.Where(i => i.GroupId == request.GroupId.Value);

        if (request.PeerUserId.HasValue)
            query = query.Where(i => i.PeerUserId == request.PeerUserId.Value);

        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return await MapListAsync(items);
    }

    public async Task<IocStatsResponse> GetStatsAsync(int userId)
    {
        var counts = await VisibleIocs(userId)
            .Where(i => i.Status == "Open" || i.Status == "Investigating")
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var openCount = counts.FirstOrDefault(x => x.Status == "Open")?.Count ?? 0;
        var investigatingCount = counts.FirstOrDefault(x => x.Status == "Investigating")?.Count ?? 0;

        return new IocStatsResponse
        {
            OpenCount = openCount,
            InvestigatingCount = investigatingCount,
            ActiveCount = openCount + investigatingCount
        };
    }

    public async Task<IocResponse?> GetByIdAsync(int userId, int id)
    {
        var ioc = await VisibleIocs(userId)
            .FirstOrDefaultAsync(i => i.Id == id);

        return ioc is null ? null : await MapAsync(ioc);
    }

    public async Task<(IocResponse? Ioc, string? Error)> UpdateAsync(
        int userId,
        int id,
        UpdateIocRequest request)
    {
        var ioc = await _db.IocIndicators.FirstOrDefaultAsync(i => i.Id == id);

        if (ioc is null)
            return (null, "IOC kh�ng t?n t?i.");

        if (!await CanManageIocAsync(userId, ioc))
            return (null, "B?n kh�ng c� quy?n ch?nh s?a IOC n�y.");

        if (!string.IsNullOrWhiteSpace(request.Type) || !string.IsNullOrWhiteSpace(request.Value))
        {
            var type = string.IsNullOrWhiteSpace(request.Type) ? ioc.Type : request.Type;
            var value = string.IsNullOrWhiteSpace(request.Value) ? ioc.Value : request.Value;

            var normalized = NormalizeAndValidate(type!, value!);

            if (!normalized.Valid)
                return (null, normalized.Error);

            var duplicateQuery = _db.IocIndicators.Where(x =>
                x.Id != id &&
                x.Type == normalized.Type &&
                x.Value == normalized.Value &&
                x.GroupId == ioc.GroupId &&
                x.PeerUserId == ioc.PeerUserId);

            if (!ioc.GroupId.HasValue && !ioc.PeerUserId.HasValue)
                duplicateQuery = duplicateQuery.Where(x => x.CreatedByUserId == userId);

            if (await duplicateQuery.AnyAsync())
                return (null, "IOC n�y d� t?n t?i trong ph?m vi hi?n t?i.");

            ioc.Type = normalized.Type;
            ioc.Value = normalized.Value;
        }

        if (request.Description != null)
            ioc.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        if (!string.IsNullOrWhiteSpace(request.Severity))
            ioc.Severity = NormalizeSeverity(request.Severity);

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            ioc.Status = NormalizeStatus(request.Status);
            ioc.ResolvedAt = IsResolvedStatus(ioc.Status) ? DateTime.UtcNow : null;
        }

        if (!string.IsNullOrWhiteSpace(request.Source))
            ioc.Source = request.Source.Trim();

        if (request.Tags != null)
            ioc.Tags = string.IsNullOrWhiteSpace(request.Tags) ? null : request.Tags.Trim();

        ioc.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (await MapAsync(ioc), null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int userId, int id)
    {
        var ioc = await _db.IocIndicators.FirstOrDefaultAsync(i => i.Id == id);

        if (ioc is null)
            return (false, "IOC kh�ng t?n t?i.");

        if (!await CanManageIocAsync(userId, ioc))
            return (false, "B?n kh�ng c� quy?n x�a IOC n�y.");

        ioc.IsDeleted = true;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(IocResponse? Ioc, string? Error)> UpdateStatusAsync(
        int userId,
        int id,
        string status)
    {
        var ioc = await _db.IocIndicators.FirstOrDefaultAsync(i => i.Id == id);

        if (ioc is null)
            return (null, "IOC kh�ng t?n t?i.");

        if (!await CanManageIocAsync(userId, ioc))
            return (null, "B?n kh�ng c� quy?n c?p nh?t IOC n�y.");

        var normalizedStatus = NormalizeStatus(status);

        ioc.Status = normalizedStatus;
        ioc.ResolvedAt = IsResolvedStatus(normalizedStatus) ? DateTime.UtcNow : null;
        ioc.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (await MapAsync(ioc), null);
    }

    private IQueryable<IocIndicator> VisibleIocs(int userId)
    {
        var groupIds = _db.GroupMembers
            .Where(gm => gm.UserId == userId)
            .Select(gm => gm.GroupId);

        return _db.IocIndicators.Where(i =>
            !i.IsDeleted &&
            (i.GroupId.HasValue && groupIds.Contains(i.GroupId.Value)) ||
            (!i.GroupId.HasValue &&
                (i.CreatedByUserId == userId || i.PeerUserId == userId)));
    }

    private async Task<bool> CanManageIocAsync(int userId, IocIndicator ioc)
    {
        if (ioc.GroupId.HasValue)
        {
            var member = await _db.GroupMembers.FirstOrDefaultAsync(gm =>
                gm.GroupId == ioc.GroupId.Value &&
                gm.UserId == userId);

            if (member is null)
                return false;

            return ioc.CreatedByUserId == userId ||
                   member.Role == GroupMemberRole.Admin ||
                   member.Role == GroupMemberRole.CoAdmin;
        }

        return ioc.CreatedByUserId == userId;
    }

    private async Task<(bool Success, string? Error)> ValidateCreateScopeAsync(
        int userId,
        int? groupId,
        int? peerUserId)
    {
        if (groupId.HasValue && peerUserId.HasValue)
            return (false, "IOC ch? du?c g?n v?i group ho?c DM, kh�ng du?c ch?n c? hai.");

        if (groupId.HasValue)
        {
            var isMember = await _db.GroupMembers.AnyAsync(gm =>
                gm.GroupId == groupId.Value &&
                gm.UserId == userId);

            if (!isMember)
                return (false, "B?n kh�ng c�n l� th�nh vi�n nh�m n�n kh�ng th? t?o IOC cho nh�m n�y.");
        }

        if (peerUserId.HasValue)
        {
            if (peerUserId.Value == userId)
                return (false, "PeerUserId kh�ng h?p l?.");

            var peerExists = await _db.Users.AnyAsync(u => u.Id == peerUserId.Value);

            if (!peerExists)
                return (false, "Ngu?i d�ng DM kh�ng t?n t?i.");
        }

        return (true, null);
    }

    private static (string Value, string Note) SplitFirstToken(string text)
    {
        var trimmed = text.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
            return (string.Empty, string.Empty);

        var firstSpace = trimmed.IndexOf(' ');

        if (firstSpace < 0)
            return (trimmed, string.Empty);

        return (trimmed[..firstSpace], trimmed[(firstSpace + 1)..].Trim());
    }

    private static (bool Success, string Type, string Value, string Severity, string Description, string? Error) ParseCommand(string? rawCommand)
    {
        if (string.IsNullOrWhiteSpace(rawCommand))
            return (false, string.Empty, string.Empty, "Medium", string.Empty, "Command IOC r?ng.");

        var match = IocCommandRegex.Match(rawCommand.Trim());

        if (!match.Success)
        {
            var missingValueMatch = IocCommandMissingValueRegex.Match(rawCommand.Trim());

            if (missingValueMatch.Success)
            {
                return (true, missingValueMatch.Groups["type"].Value.Trim(), string.Empty, "Medium", string.Empty, null);
            }

            return (false, string.Empty, string.Empty, "Medium", string.Empty, "C� ph�p IOC kh�ng h?p l?. V� d?: /ioc cve CVE-2025-70149");
        }

        var type = match.Groups["type"].Value.Trim();
        var rest = match.Groups["rest"].Value.Trim();
        var (value, note) = SplitFirstToken(rest);
        var (severity, description) = ExtractSeverity(note);

        return (true, type, value, severity, description, null);
    }

    private static (string Severity, string Description) ExtractSeverity(string note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return ("Medium", string.Empty);

        var (first, rest) = SplitFirstToken(note);

        var severity = NormalizeSeverity(first);

        if (IsSeverity(first))
            return (severity, rest);

        return ("Medium", note.Trim());
    }

    private static bool IsSeverity(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();

        return normalized is "info" or "low" or "medium" or "high" or "critical";
    }

    private static (bool Valid, string Type, string Value, string? Error) NormalizeAndValidate(
        string? rawType,
        string? rawValue)
    {
        var type = NormalizeType(rawType);
        var value = rawValue?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return (false, type, value, "Gi� tr? IOC kh�ng du?c d? tr?ng.");

        if (type == "HASH")
        {
            if (Md5Regex.IsMatch(value))
                type = "MD5";
            else if (Sha256Regex.IsMatch(value))
                type = "SHA256";
            else
                return (false, type, value, "Hash kh�ng h?p l?. H? tr? MD5 ho?c SHA256.");
        }

        if (type == "CVE")
        {
            var match = CveRegex.Match(value);

            if (!match.Success || !value.StartsWith("CVE-", StringComparison.OrdinalIgnoreCase))
                return (false, type, value, "CVE kh�ng h?p l?. V� d?: /ioc cve CVE-2025-70149");

            value = $"CVE-{match.Groups["year"].Value}-{match.Groups["id"].Value}";
        }
        else if (type == "IP")
        {
            if (!Ipv4Regex.IsMatch(value))
                return (false, type, value, "IPv4 kh�ng h?p l?.");
        }
        else if (type == "MD5")
        {
            if (!Md5Regex.IsMatch(value))
                return (false, type, value, "MD5 kh�ng h?p l?.");
        }
        else if (type == "SHA256")
        {
            if (!Sha256Regex.IsMatch(value))
                return (false, type, value, "SHA256 kh�ng h?p l?.");
        }
        else if (type == "URL")
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return (false, type, value, "URL kh�ng h?p l?.");
            }
        }
        else if (type == "DOMAIN")
        {
            if (!DomainRegex.IsMatch(value))
                return (false, type, value, "Domain kh�ng h?p l?.");
        }
        else
        {
            return (false, type, value, "Lo?i IOC kh�ng du?c h? tr?.");
        }

        return (true, type, value, null);
    }

    private static string NormalizeType(string? type)
    {
        return (type ?? "IP").Trim().ToLowerInvariant() switch
        {
            "cve" => "CVE",
            "ip" => "IP",
            "ipv4" => "IP",
            "md5" => "MD5",
            "sha256" => "SHA256",
            "hash" => "HASH",
            "url" => "URL",
            "domain" => "DOMAIN",
            _ => "IP"
        };
    }

    private static string NormalizeSeverity(string? severity)
    {
        return (severity ?? "Medium").Trim().ToLowerInvariant() switch
        {
            "info" => "Info",
            "low" => "Low",
            "medium" => "Medium",
            "high" => "High",
            "critical" => "Critical",
            _ => "Medium"
        };
    }

    private static string NormalizeStatus(string? status)
    {
        return (status ?? "Open").Trim().ToLowerInvariant() switch
        {
            "open" => "Open",
            "investigating" => "Investigating",
            "resolved" => "Resolved",
            "falsepositive" => "FalsePositive",
            "false-positive" => "FalsePositive",
            "false_positive" => "FalsePositive",
            _ => "Open"
        };
    }

    private static bool IsResolvedStatus(string status)
    {
        return status is "Resolved" or "FalsePositive";
    }

    private static string BuildExternalUrl(string type, string value)
    {
        return type switch
        {
            "CVE" => $"https://nvd.nist.gov/vuln/detail/{value}",
            "IP" => $"https://www.virustotal.com/gui/ip-address/{value}",
            "MD5" => $"https://www.virustotal.com/gui/file/{value}",
            "SHA256" => $"https://www.virustotal.com/gui/file/{value}",
            "URL" => value,
            "DOMAIN" => $"https://www.virustotal.com/gui/domain/{value}",
            _ => string.Empty
        };
    }

    private async Task<List<IocResponse>> MapListAsync(List<IocIndicator> items)
    {
        if (items.Count == 0) return new List<IocResponse>();

        // Gom tên nhóm & tên người chat thành 2 truy vấn (thay vì 2 truy vấn cho mỗi IOC — N+1)
        var groupIds = items.Where(i => i.GroupId.HasValue).Select(i => i.GroupId!.Value).Distinct().ToList();
        var peerIds  = items.Where(i => i.PeerUserId.HasValue).Select(i => i.PeerUserId!.Value).Distinct().ToList();

        var groupNames = groupIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Groups.AsNoTracking().Where(g => groupIds.Contains(g.Id))
                .ToDictionaryAsync(g => g.Id, g => g.Name);

        var peerNames = peerIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Users.AsNoTracking().Where(u => peerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName);

        return items.Select(ioc => new IocResponse
        {
            Id = ioc.Id,
            Type = ioc.Type,
            Value = ioc.Value,
            Description = ioc.Description,
            Severity = ioc.Severity,
            Status = ioc.Status,
            Source = ioc.Source,
            Tags = ioc.Tags,
            CreatedByUserId = ioc.CreatedByUserId,
            MessageId = ioc.MessageId,
            PeerUserId = ioc.PeerUserId,
            PeerDisplayName = ioc.PeerUserId.HasValue && peerNames.TryGetValue(ioc.PeerUserId.Value, out var pn) ? pn : null,
            GroupId = ioc.GroupId,
            GroupName = ioc.GroupId.HasValue && groupNames.TryGetValue(ioc.GroupId.Value, out var gn) ? gn : null,
            ExternalUrl = BuildExternalUrl(ioc.Type, ioc.Value),
            CreatedAt = ioc.CreatedAt,
            UpdatedAt = ioc.UpdatedAt,
            ResolvedAt = ioc.ResolvedAt
        }).ToList();
    }

    private async Task<IocResponse> MapAsync(IocIndicator ioc)
    {
        string? groupName = null;
        string? peerDisplayName = null;

        if (ioc.GroupId.HasValue)
        {
            groupName = await _db.Groups
                .Where(g => g.Id == ioc.GroupId.Value)
                .Select(g => g.Name)
                .FirstOrDefaultAsync();
        }

        if (ioc.PeerUserId.HasValue)
        {
            peerDisplayName = await _db.Users
                .Where(u => u.Id == ioc.PeerUserId.Value)
                .Select(u => u.DisplayName)
                .FirstOrDefaultAsync();
        }

        return new IocResponse
        {
            Id = ioc.Id,
            Type = ioc.Type,
            Value = ioc.Value,
            Description = ioc.Description,
            Severity = ioc.Severity,
            Status = ioc.Status,
            Source = ioc.Source,
            Tags = ioc.Tags,
            CreatedByUserId = ioc.CreatedByUserId,
            MessageId = ioc.MessageId,
            PeerUserId = ioc.PeerUserId,
            PeerDisplayName = peerDisplayName,
            GroupId = ioc.GroupId,
            GroupName = groupName,
            ExternalUrl = BuildExternalUrl(ioc.Type, ioc.Value),
            CreatedAt = ioc.CreatedAt,
            UpdatedAt = ioc.UpdatedAt,
            ResolvedAt = ioc.ResolvedAt
        };
    }
}

