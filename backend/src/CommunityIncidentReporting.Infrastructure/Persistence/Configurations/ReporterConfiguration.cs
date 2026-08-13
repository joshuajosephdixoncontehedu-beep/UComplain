using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class ReporterConfiguration : IEntityTypeConfiguration<Reporter>
{
    public void Configure(EntityTypeBuilder<Reporter> builder)
    {
        builder.ToTable("reporters");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.WhatsAppNumberHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.MaskedContactReference).HasMaxLength(64).IsRequired();

        builder.HasIndex(x => x.WhatsAppNumberHash).IsUnique();

        builder.HasMany(x => x.IncidentReports)
            .WithOne(r => r.Reporter)
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.VerificationEvents)
            .WithOne(v => v.Reporter)
            .HasForeignKey(v => v.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
