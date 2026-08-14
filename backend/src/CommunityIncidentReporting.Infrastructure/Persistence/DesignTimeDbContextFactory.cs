using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CommunityIncidentReporting.Infrastructure.Persistence;

/// <summary>
/// Used by every `dotnet ef` CLI command (migrations add AND database update) — the
/// CLI prefers a discoverable IDesignTimeDbContextFactory over building the full Api
/// host, so this is the only place those commands ever read a connection string from,
/// regardless of what Program.cs would otherwise wire up. It reads
/// ConnectionStrings__DefaultConnection from the environment so `dotnet ef database
/// update` targets a real database when one is configured; `dotnet ef migrations add`
/// only inspects the model, so it works fine with the placeholder when no real
/// connection string is set.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Host=localhost;Database=design_time_only;Username=postgres;Password=postgres";
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new AppDbContext(optionsBuilder.Options);
    }
}
