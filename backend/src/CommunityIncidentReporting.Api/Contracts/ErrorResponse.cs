namespace CommunityIncidentReporting.Api.Contracts;

/// <summary>The error envelope documented in docs/api-contract.md.</summary>
public record ErrorResponse(ErrorBody Error);

public record ErrorBody(string Code, string Message, IReadOnlyDictionary<string, string[]>? Details = null);
