using CommunityIncidentReporting.Domain.Enums;

namespace CommunityIncidentReporting.Application.Features.Analytics.Dtos;

public record SourceChannelVerificationOutcomeDto(SourceChannel SourceChannel, VerificationDecisionResult Result, int Count);
