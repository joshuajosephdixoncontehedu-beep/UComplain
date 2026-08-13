namespace CommunityIncidentReporting.Application.Common.Exceptions;

/// <summary>
/// A request is well-formed and authorized but violates a business rule — e.g.
/// deactivating the last active SuperAdmin, or an illegal case status transition.
/// Maps to HTTP 409 Conflict.
/// </summary>
public class BusinessRuleException(string message) : Exception(message);
