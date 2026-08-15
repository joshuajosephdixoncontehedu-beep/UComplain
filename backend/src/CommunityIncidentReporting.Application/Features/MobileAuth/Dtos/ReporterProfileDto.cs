namespace CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;

/// <summary>
/// Mobile-safe reporter profile — never includes PasswordHash, OTP data, or refresh
/// token material.
/// </summary>
public record ReporterProfileDto(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    bool EmailVerified,
    bool IsActive,
    bool IsRestricted,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    string? LanguagePreference);
