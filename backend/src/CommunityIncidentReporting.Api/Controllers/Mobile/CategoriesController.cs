using CommunityIncidentReporting.Application.Features.MobileReports;
using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Mobile;

/// <summary>Read-only incident category catalogue for the mobile report wizard (api/mobile/categories).</summary>
public class CategoriesController(IMobileReportService reportService) : MobileControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MobileCategoryDto>>> GetActive(CancellationToken cancellationToken) =>
        Ok(await reportService.GetActiveCategoriesAsync(cancellationToken));
}
