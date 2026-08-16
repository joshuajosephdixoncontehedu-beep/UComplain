using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using CommunityIncidentReporting.Application.Features.ReporterAccount;
using CommunityIncidentReporting.Application.Features.ReporterAccount.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Controllers.Mobile;

/// <summary>Reporter self-service account management (api/mobile/me) — privacy, stats, profile, data export, account deletion.</summary>
public class MeController(IReporterAccountService accountService) : MobileControllerBase
{
    [HttpGet("privacy")]
    public async Task<ActionResult<ReporterPrivacySettingDto>> GetPrivacy(CancellationToken cancellationToken) =>
        Ok(await accountService.GetPrivacyAsync(CurrentReporterId, cancellationToken));

    [HttpPut("privacy")]
    public async Task<ActionResult<ReporterPrivacySettingDto>> UpdatePrivacy(
        [FromBody] UpdateReporterPrivacySettingRequest request, CancellationToken cancellationToken) =>
        Ok(await accountService.UpdatePrivacyAsync(CurrentReporterId, request, cancellationToken));

    [HttpGet("stats")]
    public async Task<ActionResult<ReporterStatsDto>> GetStats(CancellationToken cancellationToken) =>
        Ok(await accountService.GetStatsAsync(CurrentReporterId, cancellationToken));

    [HttpPatch]
    public async Task<ActionResult<ReporterProfileDto>> UpdateProfile(
        [FromBody] UpdateMyProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await accountService.UpdateProfileAsync(CurrentReporterId, request, cancellationToken));

    [HttpGet("photo")]
    public async Task<ActionResult<ProfilePhotoDto>> GetPhoto(CancellationToken cancellationToken) =>
        Ok(await accountService.GetProfilePhotoAsync(CurrentReporterId, cancellationToken));

    [HttpPost("photo")]
    [RequestFormLimits(MultipartBodyLengthLimit = 6_000_000)]
    [RequestSizeLimit(6_000_000)]
    public async Task<ActionResult<ProfilePhotoDto>> UploadPhoto([FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        var uploadFile = new MediaUploadFile(file.FileName, file.ContentType, file.Length, file.OpenReadStream());
        try
        {
            return Ok(await accountService.UploadProfilePhotoAsync(CurrentReporterId, uploadFile, cancellationToken));
        }
        finally
        {
            await uploadFile.Content.DisposeAsync();
        }
    }

    [HttpDelete("photo")]
    public async Task<IActionResult> DeletePhoto(CancellationToken cancellationToken)
    {
        await accountService.RemoveProfilePhotoAsync(CurrentReporterId, cancellationToken);
        return NoContent();
    }

    [HttpPost("data-export")]
    public async Task<ActionResult<DataExportRequestDto>> RequestDataExport(CancellationToken cancellationToken) =>
        Ok(await accountService.RequestDataExportAsync(CurrentReporterId, cancellationToken));

    [HttpGet("data-export")]
    public async Task<ActionResult<DataExportRequestDto>> GetLatestDataExport(CancellationToken cancellationToken) =>
        Ok(await accountService.GetLatestDataExportAsync(CurrentReporterId, cancellationToken));

    [HttpDelete]
    public async Task<ActionResult<AccountDeletionRequestDto>> RequestDeletion(CancellationToken cancellationToken) =>
        Ok(await accountService.RequestAccountDeletionAsync(CurrentReporterId, cancellationToken));

    [HttpPost("deletion/cancel")]
    public async Task<ActionResult<AccountDeletionRequestDto>> CancelDeletion(CancellationToken cancellationToken) =>
        Ok(await accountService.CancelAccountDeletionAsync(CurrentReporterId, cancellationToken));
}
