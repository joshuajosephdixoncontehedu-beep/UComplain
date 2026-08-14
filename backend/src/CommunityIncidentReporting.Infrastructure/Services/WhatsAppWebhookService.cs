using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CommunityIncidentReporting.Application.Common.Interfaces;
using CommunityIncidentReporting.Application.Features.Webhooks;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using CommunityIncidentReporting.Infrastructure.WhatsApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CommunityIncidentReporting.Infrastructure.Services;

/// <summary>
/// Minimal WhatsApp -> report pipeline: one inbound text message becomes one Pending
/// IncidentReport, exactly as docs/whatsapp-integration-plan.md describes as the first
/// version. Deliberately no multi-turn conversation, no category/location questions —
/// those need their own session-state design (see the plan doc's explicit scope note)
/// and aren't part of this pass. Every report lands in the same verification queue a
/// portal-entered report would, and gets reclassified/de-duplicated by a human from
/// there the same way.
/// </summary>
public class WhatsAppWebhookService(
    AppDbContext db,
    IAuditLogger auditLogger,
    IHttpClientFactory httpClientFactory,
    IOptions<WhatsAppOptions> options,
    ILogger<WhatsAppWebhookService> logger) : IWhatsAppWebhookService
{
    private const string DefaultCategoryName = "Uncategorized (WhatsApp)";

    public bool VerifySignature(string rawBody, string? signatureHeaderValue)
    {
        var appSecret = options.Value.AppSecret;
        if (string.IsNullOrEmpty(appSecret) || string.IsNullOrEmpty(signatureHeaderValue))
        {
            return false;
        }

        const string prefix = "sha256=";
        if (!signatureHeaderValue.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var providedHex = signatureHeaderValue[prefix.Length..];
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
        var computedHex = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody))).ToLowerInvariant();

        // Constant-time comparison — a signature check is exactly the kind of place a
        // timing side-channel could leak information, even though the payoff here
        // (forging a webhook call) is low-severity compared to e.g. a login endpoint.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedHex), Encoding.UTF8.GetBytes(providedHex));
    }

    public async Task HandleInboundAsync(string rawBody, CancellationToken cancellationToken)
    {
        WhatsAppWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WhatsAppWebhookPayload>(rawBody);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse WhatsApp webhook payload");
            return;
        }

        var messages = (payload?.Entry ?? [])
            .SelectMany(e => e.Changes ?? [])
            .Select(c => c.Value)
            .Where(v => v?.Messages is not null)
            .SelectMany(v => v!.Messages!);

        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, cancellationToken);
        }
    }

    private async Task ProcessMessageAsync(WhatsAppMessage message, CancellationToken cancellationToken)
    {
        if (!string.Equals(message.Type, "text", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(message.Text?.Body))
        {
            logger.LogInformation(
                "Ignoring WhatsApp message {MessageId} of type {Type} — only text messages create reports in this version.",
                message.Id, message.Type);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var numberHash = HashPhoneNumber(message.From);

        var reporter = await db.Reporters.FirstOrDefaultAsync(r => r.WhatsAppNumberHash == numberHash, cancellationToken);
        if (reporter is null)
        {
            reporter = new Reporter
            {
                Id = Guid.NewGuid(),
                WhatsAppNumberHash = numberHash,
                MaskedContactReference = MaskPhoneNumber(message.From),
                VerificationStatus = VerificationStatus.Pending,
                IsRestricted = false,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Reporters.Add(reporter);
        }

        var category = await EnsureDefaultCategoryAsync(now, cancellationToken);

        var incidentOccurredAt = long.TryParse(message.Timestamp, out var unixSeconds)
            ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds)
            : now;

        var description = message.Text!.Body!.Length > 4000 ? message.Text.Body[..4000] : message.Text.Body;

        // A restricted reporter's messages are still recorded — silently dropping them
        // would tip off a bad-faith sender that they've been flagged — but start under
        // closer scrutiny rather than the default Pending path, per the plan doc.
        var report = new IncidentReport
        {
            Id = Guid.NewGuid(),
            ReporterId = reporter.Id,
            CategoryId = category.Id,
            SourceChannel = SourceChannel.WhatsApp,
            Description = description,
            IncidentOccurredAt = incidentOccurredAt,
            LocationDescription = "Not provided — submitted via WhatsApp, awaiting admin follow-up.",
            Priority = reporter.IsRestricted ? IncidentPriority.Low : category.DefaultPriority,
            VerificationStatus = reporter.IsRestricted ? VerificationStatus.FlaggedAbuse : VerificationStatus.Pending,
            CaseStatus = CaseStatus.VerificationPending,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.IncidentReports.Add(report);

        await auditLogger.LogAsync(
            adminUserId: null,
            "ReportSubmittedViaWhatsApp",
            nameof(IncidentReport),
            report.Id.ToString(),
            previousValue: null,
            newValue: new { report.CategoryId, ReporterMasked = reporter.MaskedContactReference, WhatsAppMessageId = message.Id },
            ipAddress: null,
            userAgent: "WhatsApp Cloud API webhook",
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        await SendAcknowledgmentAsync(message.From, report.CaseReference, cancellationToken);
    }

    private async Task<IncidentCategory> EnsureDefaultCategoryAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var category = await db.IncidentCategories.FirstOrDefaultAsync(c => c.Name == DefaultCategoryName, cancellationToken);
        if (category is not null)
        {
            return category;
        }

        category = new IncidentCategory
        {
            Id = Guid.NewGuid(),
            Name = DefaultCategoryName,
            Description = "Default category for reports submitted via WhatsApp before an administrator reclassifies them.",
            DefaultPriority = IncidentPriority.Medium,
            SlaHours = 48,
            IsActive = true,
            DisplayOrder = 999,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.IncidentCategories.Add(category);
        // Not saved here — Id is a client-generated Guid, so EF Core's change tracker
        // resolves it as the IncidentReport's CategoryId FK correctly even before the
        // single SaveChangesAsync at the end of ProcessMessageAsync.
        return category;
    }

    private string HashPhoneNumber(string whatsAppNumber)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(options.Value.NumberHashKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(whatsAppNumber))).ToLowerInvariant();
    }

    /// <summary>
    /// Best-effort international-looking mask (e.g. "+232 76 *** 111") — not tuned per
    /// country calling-code length, just consistent with the shape shown elsewhere in
    /// the admin UI for seeded/manually-entered reporters.
    /// </summary>
    private static string MaskPhoneNumber(string rawNumber)
    {
        var digits = new string(rawNumber.Where(char.IsDigit).ToArray());
        if (digits.Length <= 5)
        {
            return "+" + digits;
        }

        var prefixLength = Math.Max(1, digits.Length - 6);
        return $"+{digits[..prefixLength]} {digits.Substring(prefixLength, 2)} *** {digits[^3..]}";
    }

    private async Task SendAcknowledgmentAsync(string toNumber, string caseReference, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.AccessToken) || string.IsNullOrWhiteSpace(options.Value.PhoneNumberId))
        {
            logger.LogInformation("WhatsApp AccessToken/PhoneNumberId not configured — skipping outbound acknowledgment.");
            return;
        }

        try
        {
            using var client = httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://graph.facebook.com/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.AccessToken);

            var body = new
            {
                messaging_product = "whatsapp",
                to = toNumber,
                type = "text",
                text = new { body = $"Thanks — your report ({caseReference}) has been received and will be reviewed by our team shortly." }
            };

            var response = await client.PostAsJsonAsync(
                $"{options.Value.ApiVersion}/{options.Value.PhoneNumberId}/messages", body, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "WhatsApp acknowledgment send failed: {StatusCode} {Body}", response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            // Never let a failed outbound reply fail inbound processing — the report is
            // already saved, and Meta only cares that the webhook call itself returns
            // 200 within its timeout, not that a reply gets sent.
            logger.LogWarning(ex, "Failed to send WhatsApp acknowledgment for case {CaseReference}", caseReference);
        }
    }
}
