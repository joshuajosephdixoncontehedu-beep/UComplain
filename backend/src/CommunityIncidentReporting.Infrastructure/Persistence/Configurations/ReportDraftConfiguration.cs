using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class ReportDraftConfiguration : IEntityTypeConfiguration<ReportDraft>
{
    public void Configure(EntityTypeBuilder<ReportDraft> builder)
    {
        builder.ToTable("report_drafts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.LocationDescription).HasMaxLength(300);
        builder.Property(x => x.Landmark).HasMaxLength(300);

        builder.HasIndex(x => x.ReporterId);

        builder.HasOne(x => x.Reporter)
            .WithMany()
            .HasForeignKey(x => x.ReporterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SubmittedReport)
            .WithOne()
            .HasForeignKey<ReportDraft>(x => x.SubmittedReportId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
