namespace CommunityIncidentReporting.Application.Features.Verification.Dtos;

public record VerificationDecisionRequest(VerificationDecisionAction Action, string? Reason);
