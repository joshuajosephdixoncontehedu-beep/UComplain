using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class AccountDeletionRequestConfiguration : IEntityTypeConfiguration<AccountDeletionRequest>
{
    public void Configure(EntityTypeBuilder<AccountDeletionRequest> builder)
    {
        builder.ToTable("account_deletion_requests");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.ReporterId);

        builder.HasOne(x => x.Reporter)
            .WithMany()
            .HasForeignKey(x => x.ReporterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
