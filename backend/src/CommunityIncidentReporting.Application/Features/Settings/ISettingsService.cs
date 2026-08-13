using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Application.Features.Settings.Dtos;

namespace CommunityIncidentReporting.Application.Features.Settings;

public interface ISettingsService
{
    /// <summary>Gets the singleton settings row, creating it with sensible defaults on first call.</summary>
    Task<SettingsDto> GetAsync(CancellationToken cancellationToken);

    Task<SettingsDto> UpdateAsync(UpdateSettingsRequest request, RequestContext context, CancellationToken cancellationToken);
}
