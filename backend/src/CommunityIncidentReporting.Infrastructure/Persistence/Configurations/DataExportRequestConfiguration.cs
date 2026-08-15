using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class DataExportRequestConfiguration : IEntityTypeConfiguration<DataExportRequest>
{
    public void Configure(EntityTypeBuilder<DataExportRequest> builder)
    {
        builder.ToTable("data_export_requests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StoragePath).HasMaxLength(1000);
        builder.Property(x => x.FailureReason).HasMaxLength(1000);

        builder.HasIndex(x => new { x.ReporterId, x.RequestedAt });

        builder.HasOne(x => x.Reporter)
            .WithMany()
            .HasForeignKey(x => x.ReporterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
