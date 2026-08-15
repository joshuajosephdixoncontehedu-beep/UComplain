using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class ClarificationRequestConfiguration : IEntityTypeConfiguration<ClarificationRequest>
{
    public void Configure(EntityTypeBuilder<ClarificationRequest> builder)
    {
        builder.ToTable("clarification_requests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();

        builder.HasIndex(x => x.IncidentReportId);

        builder.HasOne(x => x.IncidentReport)
            .WithMany()
            .HasForeignKey(x => x.IncidentReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RequestedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.RequestedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
