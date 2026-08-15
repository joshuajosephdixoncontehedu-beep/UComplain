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

        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.Ignore(x => x.IsActive);
    }
}
