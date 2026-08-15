using CommunityIncidentReporting.Application.Features.PublicMap;
using CommunityIncidentReporting.Application.Features.PublicMap.Dtos;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Services;

/// <summary>
/// Two-pass geo query: a cheap SQL-translatable bounding-box pre-filter (plain
/// arithmetic on Latitude/Longitude — no trig, so EF Core can push it to Postgres),
/// then an exact Haversine distance pass in memory over the (small, radius-bounded)
/// candidate set. Avoids fighting EF Core's SQL translation of trig functions, which is
/// the whole reason this is two passes instead of one.
///
/// Visibility is re-checked here directly (VerificationStatus/CaseStatus/
/// ShowOnPublicMap), not solely inferred from the cached IncidentReport.IsPubliclyVisible
/// column — see IReportVisibilityService's doc comment. This query is the actual
/// security boundary for what an anonymous caller can see.
/// </summary>
public class PublicMapService(AppDbContext db) : IPublicMapService
{
    private const double DefaultRadiusMeters = 5000;
    private const double MinRadiusMeters = 100;
    private const double MaxRadiusMeters = 20000;
    private const int MaxResults = 500;
    private const double MetersPerDegreeLatitude = 111_320;

    public async Task<IReadOnlyList<PublicIncidentDto>> GetNearbyAsync(
        double lat, double lng, double? radiusM, Guid[]? categoryIds, CancellationToken cancellationToken)
    {
        var radius = Math.Clamp(radiusM ?? DefaultRadiusMeters, MinRadiusMeters, MaxRadiusMeters);

        var latDelta = radius / MetersPerDegreeLatitude;
        var lngDelta = radius / (MetersPerDegreeLatitude * Math.Max(Math.Cos(ToRadians(lat)), 0.01));
        var minLat = lat - latDelta;
        var maxLat = lat + latDelta;
        var minLng = lng - lngDelta;
        var maxLng = lng + lngDelta;

        var candidatesQuery = db.IncidentReports.Where(r =>
            r.VerificationStatus == VerificationStatus.Verified && r.CaseStatus != CaseStatus.Withdrawn
            && r.IsPubliclyVisible && r.Latitude != null && r.Longitude != null
            && r.Latitude >= minLat && r.Latitude <= maxLat
            && r.Longitude >= minLng && r.Longitude <= maxLng);

        if (categoryIds is { Length: > 0 })
        {
            candidatesQuery = candidatesQuery.Where(r => categoryIds.Contains(r.CategoryId));
        }

        var candidates = await candidatesQuery
            .Select(r => new
            {
                r.Id,
                r.Latitude,
                r.Longitude,
                r.CreatedAt,
                r.ReporterId,
                CategoryName = r.Category!.Name,
                CategoryIconKey = r.Category.IconKey,
                CategoryColourToken = r.Category.ColourToken
            })
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return [];
        }

        var reporterIds = candidates.Select(c => c.ReporterId).Distinct().ToList();
        var privacyByReporter = await db.ReporterPrivacySettings
            .Where(p => reporterIds.Contains(p.ReporterId))
            .ToDictionaryAsync(p => p.ReporterId, p => (p.ShowOnPublicMap, p.UsePreciseLocation), cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var results = new List<(PublicIncidentDto Dto, double Distance)>();

        foreach (var c in candidates)
        {
            // Same defaults as ReporterPrivacySetting's own property initializers when
            // the reporter has no row yet — and the live re-check this class exists for:
            // a report never surfaces here if its reporter has opted out, regardless of
            // what IsPubliclyVisible was already filtered to above.
            var (showOnPublicMap, usePreciseLocation) = privacyByReporter.TryGetValue(c.ReporterId, out var p) ? p : (true, true);
            if (!showOnPublicMap)
            {
                continue;
            }

            var distance = HaversineMeters(lat, lng, c.Latitude!.Value, c.Longitude!.Value);
            if (distance > radius)
            {
                continue;
            }

            var (displayLat, displayLng) = usePreciseLocation
                ? (c.Latitude.Value, c.Longitude.Value)
                : (Math.Round(c.Latitude.Value, 2), Math.Round(c.Longitude.Value, 2));

            results.Add((
                new PublicIncidentDto(
                    c.Id, c.CategoryName, c.CategoryIconKey, c.CategoryColourToken, displayLat, displayLng,
                    Math.Round(distance), AgeBucketFor(now - c.CreatedAt)),
                distance));
        }

        return results.OrderBy(r => r.Distance).Take(MaxResults).Select(r => r.Dto).ToList();
    }

    private static PublicIncidentAgeBucket AgeBucketFor(TimeSpan age) => age switch
    {
        _ when age < TimeSpan.FromHours(24) => PublicIncidentAgeBucket.Today,
        _ when age < TimeSpan.FromDays(7) => PublicIncidentAgeBucket.ThisWeek,
        _ when age < TimeSpan.FromDays(30) => PublicIncidentAgeBucket.ThisMonth,
        _ => PublicIncidentAgeBucket.Older
    };

    private static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusMeters = 6_371_000;
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
