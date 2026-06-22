using Markdig;
using System.Net;
using System.Text.RegularExpressions;

namespace PingMe.Frontend.Helpers;

/// <summary>
/// Converts markdown text to HTML for rendering in chat messages.
/// IOC rendering is explicit and only works with /ioc commands.
/// </summary>
public static class MarkdownHelper
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    private static readonly Regex IocCommandRegex = new(
        @"^\s*/ioc(?:\s+(?<type>[a-zA-Z0-9]+))?(?:\s+(?<value>[\s\S]+))?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CveValueRegex = new(
        @"^(?:CVE-)?(?<year>\d{4})-(?<id>\d{4,7})(?<note>[\s\S]*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Ipv4Regex = new(
        @"^(?<ip>(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d))(?<note>[\s\S]*)$",
        RegexOptions.Compiled);

    private static readonly Regex Md5Regex = new(
        @"^(?<hash>[a-fA-F0-9]{32})(?<note>[\s\S]*)$",
        RegexOptions.Compiled);

    private static readonly Regex Sha256Regex = new(
        @"^(?<hash>[a-fA-F0-9]{64})(?<note>[\s\S]*)$",
        RegexOptions.Compiled);

    private static readonly Regex UrlRegex = new(
        @"^(?<url>https?://[^\s<>()]+)(?<note>[\s\S]*)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Converts markdown to HTML.
    /// </summary>
    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        return Markdown.ToHtml(markdown, Pipeline);
    }

    /// <summary>
    /// Converts chat content to HTML.
    /// Normal messages use Markdown.
    /// IOC card only renders when the message starts with /ioc.
    /// </summary>
    public static string ToChatHtml(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        if (IsIocCommand(content))
            return RenderIocCommand(content);

        var html = Markdown.ToHtml(content, Pipeline);
        return HighlightMentionsInHtml(html);
    }

    /// <summary>
    /// True if the content likely contains markdown syntax or explicit IOC command.
    /// </summary>
    public static bool ContainsMarkdown(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        return content.Contains("```")
            || content.Contains("**")
            || content.Contains("__")
            || content.Contains("# ")
            || content.Contains("- ")
            || content.Contains("* ")
            || content.Contains("[")
            || content.Contains("|")
            || content.Contains('`')
            || IsIocCommand(content)
        || MentionRegex.IsMatch(content);
    }

    public static bool IsIocCommand(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return false;

        return content.TrimStart().StartsWith("/ioc", StringComparison.OrdinalIgnoreCase);
    }

    private static string RenderIocCommand(string content)
    {
        var match = IocCommandRegex.Match(content);

        if (!match.Success)
            return RenderIocHelp();

        var type = match.Groups["type"].Value.Trim().ToLowerInvariant();
        var value = match.Groups["value"].Value.Trim();

        if (string.IsNullOrWhiteSpace(type))
            return RenderIocHelp();

        if (type == "help")
            return RenderIocHelp();

        if (string.IsNullOrWhiteSpace(value))
            return RenderIocHelp();

        return type switch
        {
            "cve" => RenderCve(value),
            "ip" => RenderIp(value),
            "ipv4" => RenderIp(value),
            "md5" => RenderMd5(value),
            "sha256" => RenderSha256(value),
            "hash" => RenderHash(value),
            "url" => RenderUrl(value),
            _ => RenderIocError("Loại IOC không được hỗ trợ. Dùng: cve, ip, md5, sha256, hash, url.")
        };
    }

    private static string RenderCve(string value)
    {
        var match = CveValueRegex.Match(value.Trim());

        if (!match.Success)
            return RenderIocError("CVE không hợp lệ. Ví dụ: /ioc cve 2025-70149 hoặc /ioc cve CVE-2025-70149");

        var cve = $"CVE-{match.Groups["year"].Value}-{match.Groups["id"].Value}";
        var note = match.Groups["note"].Value.Trim();
        var url = $"https://nvd.nist.gov/vuln/detail/{cve}";

        return RenderIocCard(
            type: "CVE",
            value: cve,
            url: url,
            source: "NVD",
            note: note,
            icon: "🛡️");
    }

    private static string RenderIp(string value)
    {
        var match = Ipv4Regex.Match(value.Trim());

        if (!match.Success)
            return RenderIocError("IP không hợp lệ. Ví dụ: /ioc ip 8.8.8.8");

        var ip = match.Groups["ip"].Value;
        var note = match.Groups["note"].Value.Trim();
        var url = $"https://www.virustotal.com/gui/ip-address/{ip}";

        return RenderIocCard(
            type: "IP",
            value: ip,
            url: url,
            source: "VirusTotal",
            note: note,
            icon: "🌐");
    }

    private static string RenderMd5(string value)
    {
        var match = Md5Regex.Match(value.Trim());

        if (!match.Success)
            return RenderIocError("MD5 không hợp lệ. Ví dụ: /ioc md5 d41d8cd98f00b204e9800998ecf8427e");

        var hash = match.Groups["hash"].Value;
        var note = match.Groups["note"].Value.Trim();
        var url = $"https://www.virustotal.com/gui/file/{hash}";

        return RenderIocCard(
            type: "MD5",
            value: hash,
            url: url,
            source: "VirusTotal",
            note: note,
            icon: "🧬");
    }

    private static string RenderSha256(string value)
    {
        var match = Sha256Regex.Match(value.Trim());

        if (!match.Success)
            return RenderIocError("SHA256 không hợp lệ. Ví dụ: /ioc sha256 e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");

        var hash = match.Groups["hash"].Value;
        var note = match.Groups["note"].Value.Trim();
        var url = $"https://www.virustotal.com/gui/file/{hash}";

        return RenderIocCard(
            type: "SHA256",
            value: hash,
            url: url,
            source: "VirusTotal",
            note: note,
            icon: "🧬");
    }

    private static string RenderHash(string value)
    {
        var trimmed = value.Trim();

        if (Sha256Regex.IsMatch(trimmed))
            return RenderSha256(trimmed);

        if (Md5Regex.IsMatch(trimmed))
            return RenderMd5(trimmed);

        return RenderIocError("Hash không hợp lệ. Hỗ trợ MD5 32 ký tự hoặc SHA256 64 ký tự.");
    }

    private static string RenderUrl(string value)
    {
        var match = UrlRegex.Match(value.Trim());

        if (!match.Success)
            return RenderIocError("URL không hợp lệ. Ví dụ: /ioc url https://example.com/payload.exe");

        var url = TrimUrlTrailingPunctuation(match.Groups["url"].Value);
        var note = match.Groups["note"].Value.Trim();

        return RenderIocCard(
            type: "URL",
            value: url,
            url: url,
            source: "Open",
            note: note,
            icon: "🔗");
    }

    private static string RenderIocCard(
        string type,
        string value,
        string url,
        string source,
        string? note,
        string icon)
    {
        var safeType = WebUtility.HtmlEncode(type);
        var safeValue = WebUtility.HtmlEncode(value);
        var safeUrl = WebUtility.HtmlEncode(url);
        var safeSource = WebUtility.HtmlEncode(source);
        var safeIcon = WebUtility.HtmlEncode(icon);
        var safeNote = WebUtility.HtmlEncode(note ?? string.Empty);

        var typeClass = safeType.ToLowerInvariant();

        var noteHtml = string.IsNullOrWhiteSpace(safeNote)
            ? string.Empty
            : $"<div class=\"pm-ioc-note\">{safeNote}</div>";

        return $"""
        <div class="pm-ioc-card pm-ioc-{typeClass}">
            <div class="pm-ioc-header">
                <span class="pm-ioc-icon">{safeIcon}</span>
                <span class="pm-ioc-type">{safeType}</span>
                <span class="pm-ioc-source">{safeSource}</span>
            </div>

            <a href="{safeUrl}" target="_blank" rel="noopener noreferrer" class="pm-ioc-value">
                {safeValue}
            </a>

            {noteHtml}
        </div>
        """;
    }

    private static string RenderIocError(string message)
    {
        return $"""
        <div class="pm-ioc-card pm-ioc-error">
            <div class="pm-ioc-header">
                <span class="pm-ioc-icon">⚠️</span>
                <span class="pm-ioc-type">IOC</span>
                <span class="pm-ioc-source">Error</span>
            </div>

            <div class="pm-ioc-value pm-ioc-value-text">
                {WebUtility.HtmlEncode(message)}
            </div>
        </div>
        """;
    }

    private static string RenderIocHelp()
    {
        return """
        <div class="pm-ioc-card pm-ioc-help">
            <div class="pm-ioc-header">
                <span class="pm-ioc-icon">ℹ️</span>
                <span class="pm-ioc-type">IOC</span>
                <span class="pm-ioc-source">Help</span>
            </div>

            <div class="pm-ioc-help-list">
                <div><b>CVE:</b> /ioc cve 2025-70149</div>
                <div><b>IP:</b> /ioc ip 8.8.8.8</div>
                <div><b>MD5:</b> /ioc md5 d41d8cd98f00b204e9800998ecf8427e</div>
                <div><b>SHA256:</b> /ioc sha256 e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855</div>
                <div><b>URL:</b> /ioc url https://example.com/payload.exe</div>
            </div>
        </div>
        """;
    }

    private static string TrimUrlTrailingPunctuation(string value)
    {
        var url = value.Trim();

        while (url.Length > 0 && IsTrailingPunctuation(url[^1]))
            url = url[..^1];

        return url;
    }

    private static bool IsTrailingPunctuation(char c)
    {
        return c is '.' or ',' or ';' or ':' or '!' or '?';
    }
    private static readonly Regex MentionRegex = new(
        @"(?<![\w@])@(all|everyone|[a-zA-Z0-9_\-.À-ỹ]{1,40})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static string HighlightMentionsInHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var protectedSegments = new List<string>();

        string Mask(string value)
        {
            var key = $"@@PM_HTML_MASK_{protectedSegments.Count}@@";
            protectedSegments.Add(value);
            return key;
        }

        var output = Regex.Replace(
            html,
            @"<(pre|code)\b[\s\S]*?</\1>",
            match => Mask(match.Value),
            RegexOptions.IgnoreCase);

        output = MentionRegex.Replace(output, match =>
        {
            var mention = match.Value;
            var normalized = mention.ToLowerInvariant();

            var cssClass = normalized is "@all" or "@everyone"
                ? "pm-mention pm-mention-all"
                : "pm-mention pm-mention-user";

            return $"<span class=\"{cssClass}\">{WebUtility.HtmlEncode(mention)}</span>";
        });

        for (var i = 0; i < protectedSegments.Count; i++)
        {
            output = output.Replace($"@@PM_HTML_MASK_{i}@@", protectedSegments[i]);
        }

        return output;
    }
}