using System.Net.Http.Json;
using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Common.Models;
using CommunityIncidentReporting.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommunityIncidentReporting.Infrastructure.Services;

/// <summary>
/// Sends transactional email through Resend's HTTP API (https://resend.com/docs/api-reference/emails/send-email)
/// rather than SMTP — Render's free tier has no outbound SMTP support. Never logs the
/// email body (which may contain an OTP code), the API key, or the full response body
/// from Resend on failure — only the status code and a generic diagnostic.
/// </summary>
public class ResendEmailService(
    IHttpClientFactory httpClientFactory,
    IOptions<ResendOptions> options,
    ILogger<ResendEmailService> logger) : IEmailService
{
    private const string ResendClientName = "Resend";

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            logger.LogError("RESEND_API_KEY is not configured — cannot send email.");
            throw new EmailDeliveryException("Email delivery is not configured. Please contact support.");
        }

        var client = httpClientFactory.CreateClient(ResendClientName);

        var payload = new
        {
            from = string.IsNullOrWhiteSpace(opts.FromName) ? opts.FromEmail : $"{opts.FromName} <{opts.FromEmail}>",
            to = new[] { message.ToEmail },
            subject = message.Subject,
            html = message.HtmlBody,
            text = message.TextBody
        };

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("emails", payload, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Resend request timed out sending to a recipient.");
            throw new EmailDeliveryException("Sending the email timed out. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Resend request failed (network error).");
            throw new EmailDeliveryException("Could not reach the email provider. Please try again.");
        }

        if (!response.IsSuccessStatusCode)
        {
            // Deliberately not logging response.Content — Resend error bodies can echo
            // back request fields, and this call site never needs the raw body to react.
            logger.LogError(
                "Resend returned {StatusCode} sending an email.", (int)response.StatusCode);
            throw new EmailDeliveryException("The email provider rejected the message. Please try again.");
        }
    }
}
