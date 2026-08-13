namespace CommunityIncidentReporting.Application.Common.Models;

/// <summary>
/// Who is calling a mutating service method and from where — extracted from
/// HttpContext by the controller (so Application/Infrastructure services never need
/// to depend on ASP.NET Core's HttpContext directly) and threaded through to whatever
/// writes the AuditLog row.
/// </summary>
public record RequestContext(Guid AdminUserId, string? IpAddress, string? UserAgent);
