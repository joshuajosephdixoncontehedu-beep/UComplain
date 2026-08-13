using CommunityIncidentReporting.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
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

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(DatabaseName));
        });
    }

    /// <summary>
    /// A standalone context pointed at the same named InMemory database the host uses —
    /// InMemory databases are keyed by name, so this stays in sync without going through
    /// (and having to dispose) a DI scope.
    /// </summary>
    public AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(DatabaseName).Options);
}
