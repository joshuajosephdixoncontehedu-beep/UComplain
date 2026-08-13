namespace CommunityIncidentReporting.Application.Common.Exceptions;

/// <summary>
/// Login failed, or a refresh/access token is missing, expired, revoked, or otherwise
/// invalid. Deliberately generic — never indicates whether the email or the password
/// was the wrong part, to avoid leaking which admin accounts exist. Maps to HTTP 401.
/// </summary>
public class InvalidCredentialsException(string message) : Exception(message);
