using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class ReporterPrivacySettingConfiguration : IEntityTypeConfiguration<ReporterPrivacySetting>
{
    public void Configure(EntityTypeBuilder<ReporterPrivacySetting> builder)
    {
        builder.ToTable("reporter_privacy_settings");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.ReporterId).IsUnique();

        builder.HasOne(x => x.Reporter)
            .WithOne(r => r.PrivacySetting)
            .HasForeignKey<ReporterPrivacySetting>(x => x.ReporterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
