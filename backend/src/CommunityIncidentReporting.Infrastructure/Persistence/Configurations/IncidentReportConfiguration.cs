using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class IncidentReportConfiguration : IEntityTypeConfiguration<IncidentReport>
{
    public void Configure(EntityTypeBuilder<IncidentReport> builder)
    {
        builder.ToTable("incident_reports");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CaseReference)
            .HasMaxLength(32)
            .IsRequired()
            // Atomic at the Postgres level via nextval() — no app-side race window,
            // unlike computing "max + 1" in C# before SaveChanges.
            .HasDefaultValueSql(
                "'CIRS-' || EXTRACT(YEAR FROM now()) || '-' || LPAD(nextval('case_reference_seq')::text, 6, '0')")
            .ValueGeneratedOnAdd();

        builder.HasIndex(x => x.CaseReference).IsUnique();

        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.LocationDescription).HasMaxLength(300).IsRequired();
        builder.Property(x => x.MediaReference).HasMaxLength(500);
        builder.Property(x => x.ResolutionSummary).HasMaxLength(4000);

        builder.HasIndex(x => x.VerificationStatus);
        builder.HasIndex(x => x.CaseStatus);
        builder.HasIndex(x => x.Priority);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne(x => x.Reporter)
            .WithMany(r => r.IncidentReports)
            .HasForeignKey(x => x.ReporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany(c => c.IncidentReports)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedAdmin)
            .WithMany(a => a.AssignedReports)
            .HasForeignKey(x => x.AssignedAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.VerificationEvents)
            .WithOne(v => v.IncidentReport)
            .HasForeignKey(v => v.IncidentReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ReportAssignments)
            .WithOne(a => a.IncidentReport)
            .HasForeignKey(a => a.IncidentReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.StatusHistories)
            .WithOne(s => s.IncidentReport)
            .HasForeignKey(s => s.IncidentReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.InternalNotes)
            .WithOne(n => n.IncidentReport)
            .HasForeignKey(n => n.IncidentReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
