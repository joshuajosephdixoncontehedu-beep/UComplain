using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class VerificationEventConfiguration : IEntityTypeConfiguration<VerificationEvent>
{
    public void Configure(EntityTypeBuilder<VerificationEvent> builder)
    {
        builder.ToTable("verification_events");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.HasOne(x => x.PerformedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.PerformedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.IncidentReportId);
    }
}
