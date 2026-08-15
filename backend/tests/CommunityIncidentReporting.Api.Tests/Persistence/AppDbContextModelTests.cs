using CommunityIncidentReporting.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CommunityIncidentReporting.Api.Tests.Persistence;

/// <summary>
/// A cheap smoke test that the EF Core model (entity configurations, enum-as-string
/// conversions, the case-reference sequence) builds without throwing. It never opens a
/// real connection — Postgres-specific SQL like the CaseReference default is only
/// embedded as a string here, not executed, so this cannot catch a Postgres syntax
/// error, but it does catch mapping/configuration mistakes.
/// </summary>
public class AppDbContextModelTests
{
    [Fact]
    public void Model_BuildsWithoutThrowing()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_validation_only;Username=postgres;Password=postgres")
            .Options;

        using var context = new AppDbContext(options);

        var act = () => context.Model.GetEntityTypes().ToList();

        act.Should().NotThrow();
        context.Model.GetEntityTypes().Should().HaveCount(14);
    }
}
