using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class ReportInformationAdditionConfiguration : IEntityTypeConfiguration<ReportInformationAddition>
{
    public void Configure(EntityTypeBuilder<ReportInformationAddition> builder)
    {
        builder.ToTable("report_information_additions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();

        builder.HasIndex(x => x.IncidentReportId);

        builder.HasOne(x => x.IncidentReport)
            .WithMany()
            .HasForeignKey(x => x.IncidentReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Reporter)
            .WithMany()
            .HasForeignKey(x => x.ReporterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Attachment)
            .WithMany()
            .HasForeignKey(x => x.AttachmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
