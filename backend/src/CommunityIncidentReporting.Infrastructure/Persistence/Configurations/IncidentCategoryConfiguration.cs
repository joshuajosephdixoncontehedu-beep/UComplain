using CommunityIncidentReporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIncidentReporting.Infrastructure.Persistence.Configurations;

public class IncidentCategoryConfiguration : IEntityTypeConfiguration<IncidentCategory>
{
    public void Configure(EntityTypeBuilder<IncidentCategory> builder)
    {
        builder.ToTable("incident_categories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasMany(x => x.IncidentReports)
            .WithOne(r => r.Category)
            .HasForeignKey(r => r.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
