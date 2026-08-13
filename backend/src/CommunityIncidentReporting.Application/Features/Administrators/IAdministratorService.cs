using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Administrators.Dtos;

namespace CommunityIncidentReporting.Application.Features.Administrators;

public interface IAdministratorService
{
    Task<IReadOnlyList<AdministratorDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<AdministratorDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Throws BusinessRuleException if the email is already in use.</summary>
    Task<AdministratorDto> CreateAsync(
        CreateAdministratorRequest request, RequestContext context, CancellationToken cancellationToken);

    Task<AdministratorDto> UpdateAsync(
        Guid id, UpdateAdministratorRequest request, RequestContext context, CancellationToken cancellationToken);

    /// <summary>Throws BusinessRuleException if this would deactivate the last active SuperAdmin.</summary>
    Task<AdministratorDto> DeactivateAsync(Guid id, RequestContext context, CancellationToken cancellationToken);

    Task<AdministratorDto> ReactivateAsync(Guid id, RequestContext context, CancellationToken cancellationToken);
}
