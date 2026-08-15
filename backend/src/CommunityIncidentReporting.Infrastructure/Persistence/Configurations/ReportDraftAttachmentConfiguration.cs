using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class ReportDraftAttachmentConfiguration : IEntityTypeConfiguration<ReportDraftAttachment>
{
    public void Configure(EntityTypeBuilder<ReportDraftAttachment> builder)
    {
        builder.ToTable("report_draft_attachments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(150).IsRequired();

        builder.HasIndex(x => x.StoragePath).IsUnique();
        builder.HasIndex(x => new { x.ReportDraftId, x.SortOrder });

        builder.HasOne(x => x.ReportDraft)
            .WithMany(d => d.Attachments)
            .HasForeignKey(x => x.ReportDraftId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
