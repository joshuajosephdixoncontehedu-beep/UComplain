using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CommunityIncidentReporting.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` build the model and generate a migration without
/// running the full Api host (which requires a real Supabase connection string and
/// other configuration). Never used at runtime — only by the `dotnet ef` CLI. The
/// placeholder connection string below is never connected to; migration generation only
/// inspects the model.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=design_time_only;Username=postgres;Password=postgres");
        return new AppDbContext(optionsBuilder.Options);
    }
}
