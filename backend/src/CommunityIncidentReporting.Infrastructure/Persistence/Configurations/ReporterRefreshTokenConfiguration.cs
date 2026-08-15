using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class ReporterRefreshTokenConfiguration : IEntityTypeConfiguration<ReporterRefreshToken>
{
    public void Configure(EntityTypeBuilder<ReporterRefreshToken> builder)
    {
        builder.ToTable("reporter_refresh_tokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ReplacedByTokenHash).HasMaxLength(200);

        // Explicit default so the migration backfills existing rows to the same value
        // the entity's C# property initializer already gives every new row (same
        // reasoning as Reporter.IsActive — see ReporterConfiguration.cs). Every token
        // issued before this field existed was, in effect, a "remembered" 30-day one.
        builder.Property(x => x.IsRemembered).HasDefaultValue(true);

        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.Ignore(x => x.IsActive);
    }
}
