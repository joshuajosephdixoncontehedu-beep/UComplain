using System.Net;
using System.Security.Cryptography;
using System.Text;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using FluentAssertions;

namespace CommunityIncidentReporting.Api.Tests.Integration;

public class WhatsAppWebhookTests : IAsyncLifetime
{
    private const string AppSecret = "integration-test-app-secret"; // must match appsettings.Testing.json
    private const string VerifyToken = "integration-test-verify-token";

    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private static string Sign(string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(AppSecret));
        var hex = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        return $"sha256={hex}";
    }

    private static string TextMessagePayload(string from, string messageId, string body, long? unixTimestamp = null) => $$"""
        {
          "entry": [
            {
              "changes": [
                {
                  "value": {
                    "messages": [
                      {
                        "from": "{{from}}",
                        "id": "{{messageId}}",
                        "timestamp": "{{unixTimestamp ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds()}}",
                        "type": "text",
                        "text": { "body": "{{body}}" }
                      }
                    ]
                  }
                }
              ]
            }
          ]
        }
        """;

    [Fact]
    public async Task Get_WithMatchingVerifyToken_EchoesTheChallenge()
    {
        var response = await _client.GetAsync(
            $"/api/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token={VerifyToken}&hub.challenge=abc123");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("abc123");
    }

    [Fact]
    public async Task Get_WithWrongVerifyToken_Returns403()
    {
        var response = await _client.GetAsync(
            "/api/webhooks/whatsapp?hub.mode=subscribe&hub.verify_token=wrong&hub.challenge=abc123");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_WithoutASignature_Returns401AndCreatesNothing()
    {
        var body = TextMessagePayload("232761112222", "wamid.unsigned", "There is a fire near the market.");

        var response = await _client.PostAsync(
            "/api/webhooks/whatsapp", new StringContent(body, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await using var db = _factory.CreateDbContext();
        db.IncidentReports.Should().BeEmpty();
    }

    [Fact]
    public async Task Post_WithAValidSignature_CreatesAReporterAndAPendingReport()
    {
        var body = TextMessagePayload("232761112222", "wamid.first", "There is a fire near the market.");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/whatsapp")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Hub-Signature-256", Sign(body));

        var response = await _client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, responseBody);

        await using var db = _factory.CreateDbContext();
        var reporter = db.Reporters.Single();
        reporter.MaskedContactReference.Should().NotBeNullOrWhiteSpace();
        reporter.WhatsAppNumberHash.Should().NotBe("232761112222", "the raw number must never be stored");

        var report = db.IncidentReports.Single();
        report.ReporterId.Should().Be(reporter.Id);
        report.Description.Should().Be("There is a fire near the market.");
        report.SourceChannel.Should().Be(SourceChannel.WhatsApp);
        report.VerificationStatus.Should().Be(VerificationStatus.Pending);
        report.CaseStatus.Should().Be(CaseStatus.VerificationPending);

        var category = db.IncidentCategories.Single();
        category.Name.Should().Be("Uncategorized (WhatsApp)");
        report.CategoryId.Should().Be(category.Id);

        db.AuditLogs.Should().ContainSingle(l => l.Action == "ReportSubmittedViaWhatsApp" && l.AdminUserId == null);
    }

    [Fact]
    public async Task Post_ASecondMessageFromTheSameNumber_ReusesTheExistingReporter()
    {
        var first = TextMessagePayload("232761112222", "wamid.first", "First report.");
        var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/whatsapp")
        { Content = new StringContent(first, Encoding.UTF8, "application/json") };
        firstRequest.Headers.Add("X-Hub-Signature-256", Sign(first));
        await _client.SendAsync(firstRequest);

        var second = TextMessagePayload("232761112222", "wamid.second", "Second report, same sender.");
        var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/whatsapp")
        { Content = new StringContent(second, Encoding.UTF8, "application/json") };
        secondRequest.Headers.Add("X-Hub-Signature-256", Sign(second));
        var response = await _client.SendAsync(secondRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var db = _factory.CreateDbContext();
        db.Reporters.Should().ContainSingle("both messages came from the same WhatsApp number");
        db.IncidentReports.Should().HaveCount(2);
    }

    [Fact]
    public async Task Post_ANonTextMessage_IsAcknowledgedButCreatesNoReport()
    {
        const string body = """
            {
              "entry": [
                {
                  "changes": [
                    {
                      "value": {
                        "messages": [
                          { "from": "232761112222", "id": "wamid.image", "type": "image" }
                        ]
                      }
                    }
                  ]
                }
              ]
            }
            """;
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/webhooks/whatsapp")
        { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        request.Headers.Add("X-Hub-Signature-256", Sign(body));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var db = _factory.CreateDbContext();
        db.IncidentReports.Should().BeEmpty();
    }
}
