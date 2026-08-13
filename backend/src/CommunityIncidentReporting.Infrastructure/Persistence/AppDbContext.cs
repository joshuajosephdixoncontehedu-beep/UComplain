using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Reporter> Reporters => Set<Reporter>();
    public DbSet<IncidentCategory> IncidentCategories => Set<IncidentCategory>();
    public DbSet<IncidentReport> IncidentReports => Set<IncidentReport>();
    public DbSet<VerificationEvent> VerificationEvents => Set<VerificationEvent>();
    public DbSet<ReportAssignment> ReportAssignments => Set<ReportAssignment>();
    public DbSet<StatusHistory> StatusHistories => Set<StatusHistory>();
    public DbSet<InternalNote> InternalNotes => Set<InternalNote>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Enums are stored as text, not native Postgres enum types, so Supabase Studio,
        // PostgREST, and other tools that read this database directly don't need to
        // know about EF-managed Postgres enum types, and adding a new enum member never
        // requires an ALTER TYPE migration.
        configurationBuilder.Properties<AdminRole>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<VerificationStatus>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<CaseStatus>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<IncidentPriority>().HaveConversion<string>().HaveMaxLength(16);
        configurationBuilder.Properties<SourceChannel>().HaveConversion<string>().HaveMaxLength(16);
        configurationBuilder.Properties<VerificationMethod>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<VerificationDecisionResult>().HaveConversion<string>().HaveMaxLength(32);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Backs IncidentReport.CaseReference (e.g. "CIRS-2026-000001"). nextval() is
        // atomic in Postgres, so this avoids the race window of computing "max + 1" in
        // application code before SaveChanges.
        modelBuilder.HasSequence<long>("case_reference_seq").StartsAt(1).IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
