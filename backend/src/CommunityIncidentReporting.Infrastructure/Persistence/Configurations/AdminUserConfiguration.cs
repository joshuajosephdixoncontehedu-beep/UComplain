using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("admin_users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => x.Email).IsUnique();

        builder.HasMany(x => x.AssignedReports)
            .WithOne(r => r.AssignedAdmin)
            .HasForeignKey(r => r.AssignedAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.RefreshTokens)
            .WithOne(t => t.AdminUser)
            .HasForeignKey(t => t.AdminUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
