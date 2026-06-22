using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using PingMe.Data;
using PingMe.DTOs.User;

namespace PingMe.Services;

public interface IUserService
{
    Task<UserProfileResponse?> GetProfileAsync(int userId);
    Task<UserProfileResponse?> GetProfileByUsernameAsync(string username);
    Task<(bool Success, string? Error, UserProfileResponse? Profile)> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task<(bool Success, string? Error, string? AvatarUrl)> UploadAvatarAsync(int userId, IFormFile file, string webRootPath);
}

public class UserService : IUserService
{
    private const int MaxDisplayNameLength = 100;
    private const int MaxBioLength = 500;
    private const int MaxJobTitleLength = 100;
    private const int MaxDepartmentLength = 100;
    private const int MaxWorkLocationLength = 150;
    private const int MaxPhoneNumberLength = 30;

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public UserService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<UserProfileResponse?> GetProfileAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user is null ? null : MapToResponse(user);
    }

    public async Task<UserProfileResponse?> GetProfileByUsernameAsync(string username)
    {
        username = username.Trim();

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);

        return user is null ? null : MapToResponse(user);
    }

    public async Task<(bool Success, string? Error, UserProfileResponse? Profile)> UpdateProfileAsync(
        int userId,
        UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(userId);

        if (user is null)
            return (false, "User không tồn tại.", null);

        var displayName = request.DisplayName?.Trim();

        if (string.IsNullOrWhiteSpace(displayName))
            return (false, "Tên hiển thị không được để trống.", null);

        if (displayName.Length > MaxDisplayNameLength)
            return (false, $"Tên hiển thị tối đa {MaxDisplayNameLength} ký tự.", null);

        var bio = NormalizeOptional(request.Bio, MaxBioLength, "Giới thiệu", out var bioError);
        if (bioError is not null) return (false, bioError, null);

        var jobTitle = NormalizeOptional(request.JobTitle, MaxJobTitleLength, "Chức danh", out var jobError);
        if (jobError is not null) return (false, jobError, null);

        var department = NormalizeOptional(request.Department, MaxDepartmentLength, "Phòng ban", out var departmentError);
        if (departmentError is not null) return (false, departmentError, null);

        var workLocation = NormalizeOptional(request.WorkLocation, MaxWorkLocationLength, "Địa điểm làm việc", out var locationError);
        if (locationError is not null) return (false, locationError, null);

        var phoneNumber = NormalizeOptional(request.PhoneNumber, MaxPhoneNumberLength, "Số điện thoại", out var phoneError);
        if (phoneError is not null) return (false, phoneError, null);

        user.DisplayName = displayName;
        user.Bio = bio;
        user.JobTitle = jobTitle;
        user.Department = department;
        user.WorkLocation = workLocation;
        user.PhoneNumber = phoneNumber;
        user.DateOfBirth = request.DateOfBirth;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (true, null, MapToResponse(user));
    }

    public async Task<(bool Success, string? Error, string? AvatarUrl)> UploadAvatarAsync(
        int userId,
        IFormFile file,
        string webRootPath)
    {
        var user = await _db.Users.FindAsync(userId);

        if (user is null)
            return (false, "User không tồn tại.", null);

        if (file is null || file.Length <= 0)
            return (false, "Không có file ảnh.", null);

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;

        if (!allowedTypes.Contains(contentType))
            return (false, "Chỉ chấp nhận file JPEG, PNG, WebP.", null);

        if (file.Length > 5 * 1024 * 1024)
            return (false, "File tối đa 5MB.", null);

        var avatarDir = Path.Combine(webRootPath, "uploads", "avatars");
        Directory.CreateDirectory(avatarDir);

        var fileName = $"{userId}_{Guid.NewGuid():N}.jpg";
        var filePath = Path.Combine(avatarDir, fileName);

        try
        {
            await using var stream = file.OpenReadStream();
            using var image = await Image.LoadAsync(stream);

            var size = int.Parse(_config["FileStorage:AvatarSizePx"] ?? "256");

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(size, size),
                Mode = ResizeMode.Crop
            }));

            await image.SaveAsJpegAsync(filePath);
        }
        catch (UnknownImageFormatException)
        {
            return (false, "Định dạng ảnh không được hỗ trợ.", null);
        }
        catch (IOException)
        {
            return (false, "Không thể lưu ảnh đại diện.", null);
        }

        var avatarUrl = $"/uploads/avatars/{fileName}";

        user.AvatarUrl = avatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return (true, null, avatarUrl);
    }

    private static string? NormalizeOptional(string? value, int maxLength, string fieldName, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            error = $"{fieldName} tối đa {maxLength} ký tự.";
            return null;
        }

        return normalized;
    }

    private static UserProfileResponse MapToResponse(Models.User u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        DisplayName = u.DisplayName,
        AvatarUrl = u.AvatarUrl,
        Bio = u.Bio,
        JobTitle = u.JobTitle,
        Department = u.Department,
        WorkLocation = u.WorkLocation,
        PhoneNumber = u.PhoneNumber,
        DateOfBirth = u.DateOfBirth,
        IsOnline = u.IsOnline,
        LastSeen = u.LastSeen,
        CreatedAt = u.CreatedAt
    };
}
