namespace CommunityIncidentReporting.Application.Common.Exceptions;

/// <summary>
/// The caller is authenticated but not permitted to perform this action given
/// business-level rules that go beyond a simple role policy (e.g. a Reviewer trying to
/// modify a report outside their permitted status transitions). Maps to HTTP 403.
/// </summary>
public class ForbiddenAccessException(string message) : Exception(message);
