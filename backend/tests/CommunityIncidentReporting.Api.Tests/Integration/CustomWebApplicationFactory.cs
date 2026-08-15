using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CommunityIncidentReporting.Api.Tests.Integration;

/// <summary>
/// Boots the real Api host for integration tests, with the Npgsql-backed AppDbContext
/// swapped for an isolated EF Core InMemory database. Uses the "Testing" environment,
/// which:
///   - loads appsettings.Testing.json (dummy, non-secret ConnectionStrings/Jwt values)
///     early enough that Program.cs's fail-fast config checks pass — WebApplicationFactory's
///     own ConfigureAppConfiguration/ConfigureServices hooks only take effect once
///     IHostBuilder.Build() runs, which is too late for validation code that executes
///     earlier in Program.cs's top-level statements;
///   - makes Infrastructure's environment.IsDevelopment() check false, so the
///     Development-only seeding hooks never run — each test seeds only what it needs.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public readonly string DatabaseName = $"integration-tests-{Guid.NewGuid()}";

    /// <summary>Captures every email the app under test tries to send — see RecordingEmailService.</summary>
    public readonly RecordingEmailService EmailService = new();

    /// <summary>Captures every object the app under test tries to store — see RecordingStorageService.</summary>
    public readonly RecordingStorageService StorageService = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Swap the real Resend-backed IEmailService for a capturing test double —
            // integration tests need the raw OTP code without a live Resend account.
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(EmailService);

            // Same idea for Supabase Storage — no live bucket in tests.
            services.RemoveAll<ISupabaseStorageService>();
            services.AddSingleton<ISupabaseStorageService>(StorageService);
            // AddDbContext registers the Npgsql UseNpgsql(...) call as an
            // IDbContextOptionsConfiguration<AppDbContext> entry (EF Core 8+, to support
            // multiple configure callbacks), separate from DbContextOptions<AppDbContext>
            // itself. Removing only the latter leaves the former in place, so EF Core still
            // combines the Npgsql configuration with the InMemory one added below and throws
            // "only a single database provider can be registered". Remove every descriptor
            // tied to AppDbContext before re-adding it against InMemory.
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(AppDbContext) ||
                d.ServiceType == typeof(IDbContextOptionsConfiguration<AppDbContext>) ||
                (d.ServiceType.FullName?.Contains("Npgsql", StringComparison.Ordinal) ?? false) ||
                (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.Ordinal) ?? false)).ToList();
            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(DatabaseName);
                // See TestCaseReferenceModelCustomizer.cs — gives InMemory a value
                // generator for IncidentReport.CaseReference, which Postgres generates
                // server-side and InMemory otherwise has no way to produce at all.
                options.ReplaceService<IModelCustomizer, TestCaseReferenceModelCustomizer>();
            });
        });
    }

    /// <summary>
    /// A standalone context pointed at the same named InMemory database the host uses —
    /// InMemory databases are keyed by name, so this stays in sync without going through
    /// (and having to dispose) a DI scope. Must apply the exact same model customization
    /// as ConfigureWebHost's AddDbContext call above (see TestCaseReferenceModelCustomizer)
    /// — two AppDbContext instances built from different models are, in effect, different
    /// InMemory stores even when given the same database name, so any mismatch here
    /// silently makes data seeded/read through this method invisible to the app's own
    /// DI-resolved context (or vice versa).
    /// </summary>
    public AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(DatabaseName)
            .ReplaceService<IModelCustomizer, TestCaseReferenceModelCustomizer>()
            .Options);
}
