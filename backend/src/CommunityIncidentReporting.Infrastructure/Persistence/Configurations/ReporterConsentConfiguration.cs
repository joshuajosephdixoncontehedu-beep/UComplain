using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class ReporterConsentConfiguration : IEntityTypeConfiguration<ReporterConsent>
{
    public void Configure(EntityTypeBuilder<ReporterConsent> builder)
    {
        builder.ToTable("reporter_consents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PolicyVersion).HasMaxLength(40).IsRequired();

        // The hot lookup path (current-state-per-type) always queries the latest row for
        // a given reporter+type.
        builder.HasIndex(x => new { x.ReporterId, x.ConsentType, x.GrantedAt });

        builder.HasOne(x => x.Reporter)
            .WithMany()
            .HasForeignKey(x => x.ReporterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
