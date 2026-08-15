using CommunityIncidentReporting.Application.Features.Clarifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommunityIncidentReporting.Infrastructure.Verification;

/// <summary>
/// The first BackgroundService in this codebase — deliberately a plain periodic timer
/// loop, not Hangfire/Quartz, matching the product brief's own fallback allowance and
/// this project's consistent minimal-dependency pattern. Sweeps immediately on startup,
/// then on AutoCloseSweepIntervalMinutes thereafter. Resolves a new DI scope per tick
/// (AppDbContext is scoped, this service is a singleton) and delegates the actual work to
/// IClarificationAutoCloseService, which is what tests call directly rather than waiting
/// on this timer.
///
/// Operational caveat: Render's free tier spins the process down after inactivity, so
/// this in-process timer won't fire reliably on a dormant instance. Not solved here —
/// flagged for a future move to Render's Cron Job feature hitting a protected endpoint,
/// same caveat already noted for Wave 2 in general.
/// </summary>
public class ClarificationAutoCloseJob(
    IServiceScopeFactory scopeFactory, IOptions<ClarificationOptions> options, ILogger<ClarificationAutoCloseJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.AutoCloseSweepIntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IClarificationAutoCloseService>();
                var closedCount = await service.CloseOverdueAsync(stoppingToken);
                if (closedCount > 0)
                {
                    logger.LogInformation(
                        "Clarification auto-close sweep closed {ClosedCount} report(s) with an unanswered clarification.",
                        closedCount);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Clarification auto-close sweep failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
