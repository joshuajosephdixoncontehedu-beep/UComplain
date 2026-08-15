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

        // WhatsAppNumberHash defaults to "" for mobile-app reporters (who have no
        // WhatsApp identity), so the unique index is filtered to real hashes only —
        // otherwise every mobile reporter would collide on the same empty-string value.
        builder.HasIndex(x => x.WhatsAppNumberHash)
            .IsUnique()
            .HasFilter("\"WhatsAppNumberHash\" <> ''");

        builder.Property(x => x.FullName).HasMaxLength(200);
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.NormalizedEmail).HasMaxLength(320);
        builder.Property(x => x.PhoneNumber).HasMaxLength(32);
        builder.Property(x => x.PasswordHash).HasMaxLength(200);
        builder.Property(x => x.RestrictionReason).HasMaxLength(500);

        // Explicit default so the migration backfills existing WhatsApp-only rows to the
        // same value the entity's C# property initializer already gives every new row —
        // without this, EF's migration defaults new columns to the CLR default (false)
        // for existing rows while new EF-inserted rows get true, silently diverging.
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        // Case-insensitive uniqueness only where a mobile account actually has an email —
        // WhatsApp-only reporters leave NormalizedEmail null and are unaffected.
        builder.HasIndex(x => x.NormalizedEmail)
            .IsUnique()
            .HasFilter("\"NormalizedEmail\" IS NOT NULL");

        builder.HasMany(x => x.IncidentReports)
            .WithOne(r => r.Reporter)
            .HasForeignKey(r => r.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.VerificationEvents)
            .WithOne(v => v.Reporter)
            .HasForeignKey(v => v.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.RefreshTokens)
            .WithOne(t => t.Reporter)
            .HasForeignKey(t => t.ReporterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
