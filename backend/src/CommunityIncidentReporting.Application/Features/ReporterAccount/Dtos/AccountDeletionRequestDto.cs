using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.ReporterAccount.Dtos;

public record AccountDeletionRequestDto(
    Guid Id, AccountDeletionStatus Status, DateTimeOffset RequestedAt, DateTimeOffset ScheduledForAt,
    DateTimeOffset? CancelledAt);
