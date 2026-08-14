using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace CommunityIncidentReporting.Api.Tests.Integration;

/// <summary>
/// IncidentReport.CaseReference is deliberately left at its CLR default (null) in
/// production code so Postgres's server-side default (nextval()-based, see
/// IncidentReportConfiguration.cs) can generate it — EF Core omits a column from the
/// INSERT exactly when the CLR value is still the type's true default. EF Core
/// InMemory has no concept of a SQL-side default at all, so without a value generator
/// it throws "required property is missing" the moment any test creates a new
/// IncidentReport through real application code (as WhatsAppWebhookService does).
///
/// Registered only for the InMemory test double via
/// `options.ReplaceService&lt;IModelCustomizer, TestCaseReferenceModelCustomizer&gt;()`
/// in CustomWebApplicationFactory — the real Npgsql-backed model configuration used in
/// Development/Production never goes through this class.
/// </summary>
public class TestCaseReferenceModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
{
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        modelBuilder.Entity<IncidentReport>()
            .Property(r => r.CaseReference)
            .HasValueGenerator<TestCaseReferenceValueGenerator>();
    }
}

public class TestCaseReferenceValueGenerator : ValueGenerator<string>
{
    public override string Next(EntityEntry entry) => $"CIRS-TEST-{Guid.NewGuid():N}"[..20];

    public override bool GeneratesTemporaryValues => false;
}
