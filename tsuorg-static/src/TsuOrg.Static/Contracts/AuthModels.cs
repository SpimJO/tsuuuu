namespace TsuOrg.Frontend.Models;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    string Email,
    string FullName,
    string Role,
    Guid UserId,
    string? College);

public sealed record RefreshRequest(string RefreshToken);

public sealed record UserProfile(
    Guid Id,
    string Email,
    string FullName,
    string? College,
    string Role,
    string? AvatarUrl = null);

public sealed record UpdateProfileRequest(string FullName);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
