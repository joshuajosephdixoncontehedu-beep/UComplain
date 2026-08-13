using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Reporters.Dtos;

namespace CommunityIncidentReporting.Application.Features.Reporters;

public interface IReporterService
{
    Task<PagedResult<ReporterListItemDto>> GetAllAsync(GetReportersQuery query, CancellationToken cancellationToken);

    Task<ReporterDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ReporterDetailDto> RestrictAsync(Guid id, RequestContext context, CancellationToken cancellationToken);

    Task<ReporterDetailDto> UnrestrictAsync(Guid id, RequestContext context, CancellationToken cancellationToken);
}
