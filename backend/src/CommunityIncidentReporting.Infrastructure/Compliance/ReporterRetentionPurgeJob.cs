using CommunityIncidentReporting.Application.Features.ReporterAccount;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommunityIncidentReporting.Infrastructure.Compliance;

/// <summary>Periodic sweep enforcing SystemSettings.ReporterDataRetentionMonths — see ClarificationAutoCloseJob for the identical shape/reasoning this mirrors.</summary>
public class ReporterRetentionPurgeJob(
    IServiceScopeFactory scopeFactory, IOptions<ComplianceOptions> options, ILogger<ReporterRetentionPurgeJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(Math.Max(1, options.Value.RetentionPurgeSweepIntervalHours));
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IReporterRetentionPurgeService>();
                await service.PurgeInactiveAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Reporter data-retention purge sweep failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
