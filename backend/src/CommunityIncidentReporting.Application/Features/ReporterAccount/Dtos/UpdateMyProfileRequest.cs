namespace CommunityIncidentReporting.Application.Features.ReporterAccount.Dtos;

/// <summary>Full-replace, same convention as every other PATCH in this API. LanguagePreference may be null to clear it.</summary>
public record UpdateMyProfileRequest(string FullName, string? LanguagePreference);
