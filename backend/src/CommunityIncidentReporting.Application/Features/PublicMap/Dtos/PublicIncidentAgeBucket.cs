namespace CommunityIncidentReporting.Application.Features.PublicMap.Dtos;

/// <summary>Coarse relative age since CreatedAt — never the exact timestamp, on the anonymous public map.</summary>
public enum PublicIncidentAgeBucket
{
    Today,
    ThisWeek,
    ThisMonth,
    Older
}
