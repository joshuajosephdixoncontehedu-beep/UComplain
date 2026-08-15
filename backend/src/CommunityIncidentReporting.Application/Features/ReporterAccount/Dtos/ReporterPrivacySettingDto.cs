namespace CommunityIncidentReporting.Application.Features.ReporterAccount.Dtos;

public record ReporterPrivacySettingDto(
    bool UsePreciseLocation, bool ShowOnPublicMap, bool AllowResponderContact, DateTimeOffset UpdatedAt);
