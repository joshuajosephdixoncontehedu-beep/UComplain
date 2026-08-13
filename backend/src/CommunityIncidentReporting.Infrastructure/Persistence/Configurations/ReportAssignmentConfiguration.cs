using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class ReportAssignmentConfiguration : IEntityTypeConfiguration<ReportAssignment>
{
    public void Configure(EntityTypeBuilder<ReportAssignment> builder)
    {
        builder.ToTable("report_assignments");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.AdminUser)
            .WithMany()
            .HasForeignKey(x => x.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.AssignedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.IncidentReportId);
    }
}
