using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using CommunityIncidentReporting.Application.Features.ReporterAccount.Dtos;

namespace CommunityIncidentReporting.Application.Features.ReporterAccount;

/// <summary>
/// Reporter self-service account management — privacy, stats, profile, data export, and
/// account deletion. The actual data-export build and account-deletion/retention
/// anonymization run as background sweeps (see IDataExportProcessorService,
/// IAccountDeletionProcessorService, IReporterRetentionPurgeService) — this interface is
/// only the reporter-facing request/read side.
/// </summary>
public interface IReporterAccountService
{
    /// <summary>Get-or-create — same pattern as SystemSettings, defaults match ReporterPrivacySetting's own property initializers.</summary>
    Task<ReporterPrivacySettingDto> GetPrivacyAsync(Guid reporterId, CancellationToken cancellationToken);

    /// <summary>
    /// A change to ShowOnPublicMap recomputes IncidentReport.IsPubliclyVisible for every
    /// one of the caller's own existing reports, not just future ones.
    /// </summary>
    Task<ReporterPrivacySettingDto> UpdatePrivacyAsync(
        Guid reporterId, UpdateReporterPrivacySettingRequest request, CancellationToken cancellationToken);

    Task<ReporterStatsDto> GetStatsAsync(Guid reporterId, CancellationToken cancellationToken);

    Task<ReporterProfileDto> UpdateProfileAsync(
        Guid reporterId, UpdateMyProfileRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Idempotent-ish: returns the caller's already-Pending/Processing request instead of
    /// queuing a second one if there is one.
    /// </summary>
    Task<DataExportRequestDto> RequestDataExportAsync(Guid reporterId, CancellationToken cancellationToken);

    /// <summary>Throws NotFoundException if the caller has never requested an export.</summary>
    Task<DataExportRequestDto> GetLatestDataExportAsync(Guid reporterId, CancellationToken cancellationToken);

    /// <summary>
    /// Idempotent-ish: returns the caller's already-Pending request instead of creating a
    /// second one. Nothing happens to the account until the grace period elapses — see
    /// AccountDeletionRequest's doc comment.
    /// </summary>
    Task<AccountDeletionRequestDto> RequestAccountDeletionAsync(Guid reporterId, CancellationToken cancellationToken);

    /// <summary>Throws BusinessRuleException if the caller has no Pending deletion request.</summary>
    Task<AccountDeletionRequestDto> CancelAccountDeletionAsync(Guid reporterId, CancellationToken cancellationToken);
}
