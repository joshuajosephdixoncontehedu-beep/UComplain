using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Categories;
using CommunityIncidentReporting.Application.Features.Categories.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class CategoryService(AppDbContext db, IAuditLogger auditLogger) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.IncidentCategories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => ToDto(c))
            .ToListAsync(cancellationToken);

    public async Task<CategoryDto> CreateAsync(
        CreateCategoryRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var category = new IncidentCategory
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            DefaultPriority = request.DefaultPriority,
            SlaHours = request.SlaHours,
            IsActive = true,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.IncidentCategories.Add(category);
        await auditLogger.LogAsync(
            context.AdminUserId, "CategoryCreated", nameof(IncidentCategory), category.Id.ToString(),
            previousValue: null, newValue: request, context.IpAddress, context.UserAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(
        Guid id, UpdateCategoryRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        var category = await db.IncidentCategories.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException(nameof(IncidentCategory), id);

        var previous = ToDto(category);

        category.Name = request.Name;
        category.Description = request.Description;
        category.DefaultPriority = request.DefaultPriority;
        category.SlaHours = request.SlaHours;
        category.DisplayOrder = request.DisplayOrder;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        await auditLogger.LogAsync(
            context.AdminUserId, "CategoryUpdated", nameof(IncidentCategory), category.Id.ToString(),
            previous, request, context.IpAddress, context.UserAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(category);
    }

    public async Task<CategoryDto> DisableAsync(Guid id, RequestContext context, CancellationToken cancellationToken)
    {
        var category = await db.IncidentCategories.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException(nameof(IncidentCategory), id);

        var previous = ToDto(category);
        category.IsActive = false;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        await auditLogger.LogAsync(
            context.AdminUserId, "CategoryDisabled", nameof(IncidentCategory), category.Id.ToString(),
            previous, ToDto(category), context.IpAddress, context.UserAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(category);
    }

    private static CategoryDto ToDto(IncidentCategory c) => new(
        c.Id, c.Name, c.Description, c.DefaultPriority, c.SlaHours, c.IsActive, c.DisplayOrder, c.CreatedAt, c.UpdatedAt);
}
