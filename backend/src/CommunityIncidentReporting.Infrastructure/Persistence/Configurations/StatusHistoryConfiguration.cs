using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class StatusHistoryConfiguration : IEntityTypeConfiguration<StatusHistory>
{
    public void Configure(EntityTypeBuilder<StatusHistory> builder)
    {
        builder.ToTable("status_histories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.HasOne(x => x.ChangedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.ChangedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.IncidentReportId);
    }
}
