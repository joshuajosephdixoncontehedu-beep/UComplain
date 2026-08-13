using CommunityIncidentReporting.Application.Features.Analytics;
using CommunityIncidentReporting.Application.Features.Analytics.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Admin;

public class AnalyticsController(IAnalyticsService analyticsService) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AnalyticsResponse>> Get(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? format,
        CancellationToken cancellationToken)
    {
        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csv = await analyticsService.ExportCsvAsync(from, to, cancellationToken);
            return File(csv, "text/csv", $"cirs-analytics-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        return Ok(await analyticsService.GetAsync(from, to, cancellationToken));
    }
}
