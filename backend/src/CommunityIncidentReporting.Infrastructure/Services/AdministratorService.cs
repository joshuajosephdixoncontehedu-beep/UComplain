using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Administrators;
using CommunityIncidentReporting.Application.Features.Administrators.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Services;

public class AdministratorService(AppDbContext db, IPasswordHasher passwordHasher, IAuditLogger auditLogger)
    : IAdministratorService
{
    public async Task<IReadOnlyList<AdministratorDto>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.AdminUsers
            .OrderBy(a => a.FullName)
            .Select(a => ToDto(a))
            .ToListAsync(cancellationToken);

    public async Task<AdministratorDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var admin = await db.AdminUsers.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException(nameof(AdminUser), id);
        return ToDto(admin);
    }

    public async Task<AdministratorDto> CreateAsync(
        CreateAdministratorRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailInUse = await db.AdminUsers.AnyAsync(a => a.Email.ToLower() == normalizedEmail, cancellationToken);
        if (emailInUse)
        {
            throw new BusinessRuleException($"An administrator with email '{request.Email}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var admin = new AdminUser
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.TemporaryPassword),
            Role = request.Role,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.AdminUsers.Add(admin);
        await auditLogger.LogAsync(
            context.AdminUserId, "AdministratorCreated", nameof(AdminUser), admin.Id.ToString(),
            previousValue: null, newValue: new { admin.FullName, admin.Email, Role = admin.Role.ToString() },
            context.IpAddress, context.UserAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(admin);
    }

    public async Task<AdministratorDto> UpdateAsync(
        Guid id, UpdateAdministratorRequest request, RequestContext context, CancellationToken cancellationToken)
    {
        var admin = await db.AdminUsers.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException(nameof(AdminUser), id);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailInUseByAnother = await db.AdminUsers
            .AnyAsync(a => a.Id != id && a.Email.ToLower() == normalizedEmail, cancellationToken);
        if (emailInUseByAnother)
        {
            throw new BusinessRuleException($"An administrator with email '{request.Email}' already exists.");
        }

        if (admin.Role == AdminRole.SuperAdmin && request.Role != AdminRole.SuperAdmin)
        {
            await EnsureNotLastActiveSuperAdminAsync(admin.Id, cancellationToken,
                "The last SuperAdmin's role cannot be changed — promote another administrator to SuperAdmin first.");
        }

        var previous = ToDto(admin);

        admin.FullName = request.FullName;
        admin.Email = request.Email;
        admin.Role = request.Role;
        admin.UpdatedAt = DateTimeOffset.UtcNow;

        await auditLogger.LogAsync(
            context.AdminUserId, "AdministratorUpdated", nameof(AdminUser), admin.Id.ToString(),
            previous, request, context.IpAddress, context.UserAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(admin);
    }

    public async Task<AdministratorDto> DeactivateAsync(Guid id, RequestContext context, CancellationToken cancellationToken)
    {
        var admin = await db.AdminUsers.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException(nameof(AdminUser), id);

        if (admin.Role == AdminRole.SuperAdmin)
        {
            await EnsureNotLastActiveSuperAdminAsync(admin.Id, cancellationToken,
                "Cannot deactivate the last active SuperAdmin account.");
        }

        var previous = ToDto(admin);
        admin.IsActive = false;
        admin.UpdatedAt = DateTimeOffset.UtcNow;

        await auditLogger.LogAsync(
            context.AdminUserId, "AdministratorDeactivated", nameof(AdminUser), admin.Id.ToString(),
            previous, ToDto(admin), context.IpAddress, context.UserAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(admin);
    }

    public async Task<AdministratorDto> ReactivateAsync(Guid id, RequestContext context, CancellationToken cancellationToken)
    {
        var admin = await db.AdminUsers.FindAsync([id], cancellationToken)
            ?? throw new NotFoundException(nameof(AdminUser), id);

        var previous = ToDto(admin);
        admin.IsActive = true;
        admin.UpdatedAt = DateTimeOffset.UtcNow;

        await auditLogger.LogAsync(
            context.AdminUserId, "AdministratorReactivated", nameof(AdminUser), admin.Id.ToString(),
            previous, ToDto(admin), context.IpAddress, context.UserAgent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(admin);
    }

    private async Task EnsureNotLastActiveSuperAdminAsync(Guid excludingId, CancellationToken cancellationToken, string message)
    {
        var otherActiveSuperAdmins = await db.AdminUsers.CountAsync(
            a => a.Id != excludingId && a.Role == AdminRole.SuperAdmin && a.IsActive, cancellationToken);
        if (otherActiveSuperAdmins == 0)
        {
            throw new BusinessRuleException(message);
        }
    }

    private static AdministratorDto ToDto(AdminUser a) => new(
        a.Id, a.FullName, a.Email, a.Role, a.IsActive, a.LastLoginAt, a.CreatedAt);
}
