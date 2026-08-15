using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.ToTable("device_tokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.Token).IsUnique();

        builder.HasIndex(x => x.ReporterId);

        builder.HasOne(x => x.Reporter)
            .WithMany()
            .HasForeignKey(x => x.ReporterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
