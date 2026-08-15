using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class IncidentMediaAttachmentConfiguration : IEntityTypeConfiguration<IncidentMediaAttachment>
{
    public void Configure(EntityTypeBuilder<IncidentMediaAttachment> builder)
    {
        builder.ToTable("incident_media_attachments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.PublicOrSignedUrlReference).HasMaxLength(1000);
        builder.Property(x => x.MimeType).HasMaxLength(150).IsRequired();

        builder.HasIndex(x => x.StoragePath).IsUnique();
        builder.HasIndex(x => new { x.IncidentReportId, x.SortOrder });

        builder.HasOne(x => x.IncidentReport)
            .WithMany(r => r.MediaAttachments)
            .HasForeignKey(x => x.IncidentReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
