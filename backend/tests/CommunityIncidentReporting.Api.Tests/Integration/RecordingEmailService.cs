using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Common.Models;

namespace CommunityIncidentReporting.Api.Tests.Integration;

/// <summary>
/// Test double for IEmailService — captures every message instead of calling Resend, so
/// integration tests can pull an OTP code out of the body text without a real email
/// provider. Registered as a singleton by CustomWebApplicationFactory so the same
/// instance is visible to both the test host and the test method.
/// </summary>
public class RecordingEmailService : IEmailService
{
    private readonly List<EmailMessage> _sent = [];
    private readonly object _lock = new();

    public IReadOnlyCollection<EmailMessage> SentMessages
    {
        get { lock (_lock) { return _sent.ToList(); } }
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        lock (_lock) { _sent.Add(message); }
        return Task.CompletedTask;
    }

    /// <summary>The most recently sent message to this address — insertion order is preserved.</summary>
    public EmailMessage LatestFor(string toEmail)
    {
        lock (_lock)
        {
            return _sent.Last(m => string.Equals(m.ToEmail, toEmail, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static string ExtractOtpCode(EmailMessage message) =>
        System.Text.RegularExpressions.Regex.Match(message.TextBody, @"\d{6}").Value;
}
