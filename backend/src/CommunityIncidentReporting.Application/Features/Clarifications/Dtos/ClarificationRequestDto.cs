namespace CommunityIncidentReporting.Application.Features.Clarifications.Dtos;

/// <summary>
/// resolvedAt is set on the reporter's first reply; autoClosedAt is set only if the
/// deadline passed with no reply at all — the two are mutually exclusive in practice.
/// </summary>
public record ClarificationRequestDto(
    Guid Id,
    string Message,
    DateTimeOffset RequestedAt,
    DateTimeOffset DueAt,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? AutoClosedAt,
    IReadOnlyList<ClarificationResponseDto> Responses);
