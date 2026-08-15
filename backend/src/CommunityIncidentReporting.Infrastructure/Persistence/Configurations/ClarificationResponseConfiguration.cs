using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class ClarificationResponseConfiguration : IEntityTypeConfiguration<ClarificationResponse>
{
    public void Configure(EntityTypeBuilder<ClarificationResponse> builder)
    {
        builder.ToTable("clarification_responses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();

        builder.HasIndex(x => x.ClarificationRequestId);

        builder.HasOne(x => x.ClarificationRequest)
            .WithMany(x => x.Responses)
            .HasForeignKey(x => x.ClarificationRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Attachment)
            .WithMany()
            .HasForeignKey(x => x.AttachmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
