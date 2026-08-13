using CommunityIncidentReporting.Api.Authorization;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.AuditLogs;
using CommunityIncidentReporting.Application.Features.AuditLogs.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Admin;

[Authorize(Policy = Policies.SuperAdminOnly)]
public class AuditLogsController(IAuditLogQueryService auditLogQueryService) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogListItemDto>>> GetAll(
        [FromQuery] GetAuditLogsQuery query, CancellationToken cancellationToken) =>
        Ok(await auditLogQueryService.GetAllAsync(query, cancellationToken));
}
