using CommunityIncidentReporting.Api.Authorization;
using CommunityIncidentReporting.Application.Features.Categories;
using CommunityIncidentReporting.Application.Features.Categories.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Admin;

public class CategoriesController(ICategoryService categoryService) : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await categoryService.GetAllAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<CategoryDto>> Create(
        [FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await categoryService.CreateAsync(request, BuildRequestContext(), cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, category);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<CategoryDto>> Update(
        Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken) =>
        Ok(await categoryService.UpdateAsync(id, request, BuildRequestContext(), cancellationToken));

    [HttpPost("{id:guid}/disable")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<CategoryDto>> Disable(Guid id, CancellationToken cancellationToken) =>
        Ok(await categoryService.DisableAsync(id, BuildRequestContext(), cancellationToken));
}
