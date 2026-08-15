namespace CommunityIncidentReporting.Application.Features.ReporterAccount.Dtos;

/// <summary>
/// Full-replace, same convention as every other PATCH/PUT in this API. Changing
/// ShowOnPublicMap recomputes IsPubliclyVisible for every one of the caller's own
/// existing reports, not just future ones — see IReporterAccountService.
/// </summary>
public record UpdateReporterPrivacySettingRequest(bool UsePreciseLocation, bool ShowOnPublicMap, bool AllowResponderContact);
