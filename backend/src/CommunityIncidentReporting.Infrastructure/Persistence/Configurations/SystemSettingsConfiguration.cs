using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.ToTable("system_settings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OrganizationContactEmail).HasMaxLength(320).IsRequired();
        builder.Property(x => x.WhatsAppPlaceholderNote).HasMaxLength(1000);
    }
}
