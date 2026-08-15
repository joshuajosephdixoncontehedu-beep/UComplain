using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class EmailOtpVerificationConfiguration : IEntityTypeConfiguration<EmailOtpVerification>
{
    public void Configure(EntityTypeBuilder<EmailOtpVerification> builder)
    {
        builder.ToTable("email_otp_verifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.CodeHash).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RequestIp).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(500);

        // The hot lookup path (EmailOtpService) always queries the latest active OTP for
        // an email+purpose pair.
        builder.HasIndex(x => new { x.Email, x.Purpose, x.IsUsed, x.ExpiresAt });

        builder.HasOne(x => x.Reporter)
            .WithMany()
            .HasForeignKey(x => x.ReporterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
