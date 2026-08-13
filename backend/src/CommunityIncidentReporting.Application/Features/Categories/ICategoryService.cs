using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Categories.Dtos;

namespace CommunityIncidentReporting.Application.Features.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, RequestContext context, CancellationToken cancellationToken);

    /// <summary>Throws NotFoundException if no category with this id exists.</summary>
    Task<CategoryDto> UpdateAsync(
        Guid id, UpdateCategoryRequest request, RequestContext context, CancellationToken cancellationToken);

    /// <summary>Soft-disables (IsActive = false); throws NotFoundException if unknown.</summary>
    Task<CategoryDto> DisableAsync(Guid id, RequestContext context, CancellationToken cancellationToken);
}
