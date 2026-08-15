using CommunityIncidentReporting.Application.Features.ReporterAccount;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommunityIncidentReporting.Infrastructure.Compliance;

/// <summary>Periodic sweep for grace-period-expired account-deletion requests — see ClarificationAutoCloseJob for the identical shape/reasoning this mirrors.</summary>
public class AccountDeletionJob(
    IServiceScopeFactory scopeFactory, IOptions<ComplianceOptions> options, ILogger<AccountDeletionJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.AccountDeletionSweepIntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IAccountDeletionProcessorService>();
                await service.ProcessDueAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Account deletion sweep failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
