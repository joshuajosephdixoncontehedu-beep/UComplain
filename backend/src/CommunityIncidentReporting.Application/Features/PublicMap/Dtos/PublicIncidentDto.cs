namespace CommunityIncidentReporting.Application.Features.PublicMap.Dtos;

/// <summary>
/// Anonymous public-map pin — deliberately never reporter identity, description, or
/// exact location text. Latitude/Longitude are coarsened (rounded to ~2 decimal places,
/// ~1.1km) when the reporter's UsePreciseLocation privacy setting is off.
/// </summary>
public record PublicIncidentDto(
    Guid Id, string CategoryName, string? CategoryIconKey, string? CategoryColourToken,
    double Latitude, double Longitude, double DistanceMeters, PublicIncidentAgeBucket AgeBucket);
