using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using System.Text.RegularExpressions;

namespace PingMe.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public FilesController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [AllowAnonymous]
    [HttpGet("download/messages/{date}/{fileName}")]
    public IActionResult DownloadMessageFile(string date, string fileName, [FromQuery] string? originalName, [FromQuery] bool inline = false)
    {
        if (string.IsNullOrWhiteSpace(date) || !Regex.IsMatch(date, @"^\d{8}$"))
            return BadRequest("Ngày không hợp lệ.");

        fileName = Path.GetFileName(Uri.UnescapeDataString(fileName));

        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest("Tên file không hợp lệ.");

        var webRoot = _env.WebRootPath;

        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");

        var uploadRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads", "messages"));
        var filePath = Path.GetFullPath(Path.Combine(uploadRoot, date, fileName));

        if (!filePath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase))
            return BadRequest("Đường dẫn file không hợp lệ.");

        if (!System.IO.File.Exists(filePath))
            return NotFound("Không tìm thấy file.");

        var provider = new FileExtensionContentTypeProvider();

        if (!provider.TryGetContentType(fileName, out var contentType))
            contentType = "application/octet-stream";

        var displayName = fileName;

        if (!string.IsNullOrWhiteSpace(originalName))
        {
            displayName = Uri.UnescapeDataString(originalName);
        }
        else
        {
            var underscoreIndex = fileName.IndexOf('_');
            if (underscoreIndex >= 0 && underscoreIndex < fileName.Length - 1)
                displayName = fileName[(underscoreIndex + 1)..];
        }

        // Loại bỏ các ký tự điều khiển (CR, LF, Tab) có thể gây lỗi HTTP Header
        displayName = Regex.Replace(displayName, @"[\r\n\t]", "");

        if (inline)
        {
            var cd = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("inline");
            cd.SetHttpFileName(displayName);
            Response.Headers.Append(Microsoft.Net.Http.Headers.HeaderNames.ContentDisposition, cd.ToString());
            return PhysicalFile(filePath, contentType, enableRangeProcessing: true);
        }

        return PhysicalFile(filePath, contentType, displayName, enableRangeProcessing: true);
    }
}