using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Infrastructure.Persistence;
using CommunityIncidentReporting.Infrastructure.Persistence.Seeding;
using CommunityIncidentReporting.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CommunityIncidentReporting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. Set ConnectionStrings__DefaultConnection " +
                "to your Supabase Postgres connection string (see backend/.env.example).");
        }

        services.AddDbContext<AppDbContext>(options =>
        {
            // EnableRetryOnFailure guards against transient network blips to Supabase.
            // Do not combine with a manually started Database.BeginTransaction() —
            // use Database.CreateExecutionStrategy().Execute(...) instead if a future
            // use case needs an explicit multi-SaveChanges transaction.
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(3));

            if (environment.IsDevelopment())
            {
                options.UseSeeding((context, _) =>
                {
                    DevelopmentSeeder.Seed((AppDbContext)context, configuration, new BCryptPasswordHasher());
                });
                options.UseAsyncSeeding(async (context, _, cancellationToken) =>
                {
                    await DevelopmentSeeder.SeedAsync(
                        (AppDbContext)context, configuration, new BCryptPasswordHasher(), cancellationToken);
                });
            }
        });

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

        return services;
    }
}
