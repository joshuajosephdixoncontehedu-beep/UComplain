namespace CommunityIncidentReporting.Application.Common.Exceptions;

/// <summary>
/// An OTP code was missing, wrong, expired, already used, or attempt-limited. Maps to
/// HTTP 400 Bad Request — deliberately generic (never distinguishes "wrong code" from
/// "expired" from "no such request") so a caller can't use the error to probe state.
/// </summary>
public class InvalidOtpException(string message) : Exception(message);
