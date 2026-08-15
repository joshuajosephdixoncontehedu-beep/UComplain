using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityIncidentReporting.Application.Features.Auth.Dtos;
using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using CommunityIncidentReporting.Domain.Entities;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Security;
using FluentAssertions;

namespace CommunityIncidentReporting.Api.Tests.Integration;

// Deliberately not IClassFixture-shared — see AuthEndpointsTests for why.
public class MobileAuthEndpointsTests : IAsyncLifetime
{
    // Matches the server's AddJsonOptions(JsonStringEnumConverter) in Program.cs — see ReportsAndVerificationTests.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CustomWebApplicationFactory _factory = new();
    private HttpClient _client = null!;
    private const string Password = "Correct-Horse-1";
    private const string Email = "reporter@example.com";

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

    private static RegisterReporterRequest ValidRegisterRequest(string email = Email) => new(
        FullName: "Aminata Kamara",
        Email: email,
        PhoneNumber: "+23276111999",
        Password: Password,
        ConfirmPassword: Password,
        ConsentAccepted: true);

    private async Task<string> RegisterAndVerifyAsync(string email = Email)
    {
        var register = await _client.PostAsJsonAsync("/api/mobile/auth/register", ValidRegisterRequest(email));
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        var message = _factory.EmailService.LatestFor(email);
        var code = RecordingEmailService.ExtractOtpCode(message);

        var verify = await _client.PostAsJsonAsync("/api/mobile/auth/verify-email-otp", new VerifyEmailOtpRequest(email, code));
        verify.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await verify.Content.ReadFromJsonAsync<ReporterAuthTokenResponse>();
        return tokens!.AccessToken;
    }

    [Fact]
    public async Task Register_ThenVerifyOtp_ActivatesAccountAndIssuesReporterTokens()
    {
        var register = await _client.PostAsJsonAsync("/api/mobile/auth/register", ValidRegisterRequest());
        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerBody = await register.Content.ReadFromJsonAsync<RegisterReporterResponse>();
        registerBody!.VerificationRequired.Should().BeTrue();

        var message = _factory.EmailService.LatestFor(Email);
        var code = RecordingEmailService.ExtractOtpCode(message);
        code.Should().HaveLength(6);

        var verify = await _client.PostAsJsonAsync("/api/mobile/auth/verify-email-otp", new VerifyEmailOtpRequest(Email, code));
        verify.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await verify.Content.ReadFromJsonAsync<ReporterAuthTokenResponse>();
        tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.Reporter.Email.Should().Be(Email);
        tokens.Reporter.EmailVerified.Should().BeTrue();
        tokens.Reporter.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyEmailOtp_WithWrongCode_Returns400AndDoesNotConsumeTheRealCode()
    {
        await _client.PostAsJsonAsync("/api/mobile/auth/register", ValidRegisterRequest());
        var message = _factory.EmailService.LatestFor(Email);
        var realCode = RecordingEmailService.ExtractOtpCode(message);

        var wrongAttempt = await _client.PostAsJsonAsync(
            "/api/mobile/auth/verify-email-otp", new VerifyEmailOtpRequest(Email, "000000"));
        wrongAttempt.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await wrongAttempt.Content.ReadAsStringAsync()).Should().Contain("invalid_otp");

        var correctAttempt = await _client.PostAsJsonAsync(
            "/api/mobile/auth/verify-email-otp", new VerifyEmailOtpRequest(Email, realCode));
        correctAttempt.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_BeforeEmailVerification_ReturnsInvalidCredentials()
    {
        await _client.PostAsJsonAsync("/api/mobile/auth/register", ValidRegisterRequest());

        var login = await _client.PostAsJsonAsync("/api/mobile/auth/login", new ReporterLoginRequest(Email, Password));

        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_AfterVerification_ReturnsTokensAndRecordsLastLoginAt()
    {
        await RegisterAndVerifyAsync();

        var login = await _client.PostAsJsonAsync("/api/mobile/auth/login", new ReporterLoginRequest(Email, Password));

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await login.Content.ReadFromJsonAsync<ReporterAuthTokenResponse>();
        tokens!.Reporter.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Me_WithAValidReporterToken_ReturnsTheMatchingProfile()
    {
        var accessToken = await RegisterAndVerifyAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _client.GetAsync("/api/mobile/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<ReporterProfileDto>();
        profile!.Email.Should().Be(Email);
    }

    [Fact]
    public async Task ForgotPassword_ForAnUnregisteredEmail_StillReturns200WithGenericMessage()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/mobile/auth/forgot-password", new ForgotPasswordRequest("nobody@example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.EmailService.SentMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task ForgotPassword_ThenResetPassword_AllowsLoginWithTheNewPassword()
    {
        await RegisterAndVerifyAsync();

        await _client.PostAsJsonAsync("/api/mobile/auth/forgot-password", new ForgotPasswordRequest(Email));
        var resetMessage = _factory.EmailService.LatestFor(Email);
        var resetCode = RecordingEmailService.ExtractOtpCode(resetMessage);

        const string newPassword = "Brand-New-Password-2";
        var reset = await _client.PostAsJsonAsync("/api/mobile/auth/reset-password",
            new ResetPasswordRequest(Email, resetCode, newPassword, newPassword));
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        var oldLogin = await _client.PostAsJsonAsync("/api/mobile/auth/login", new ReporterLoginRequest(Email, Password));
        oldLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newLogin = await _client.PostAsJsonAsync("/api/mobile/auth/login", new ReporterLoginRequest(Email, newPassword));
        newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReporterToken_CannotAccessAnAdminEndpoint()
    {
        var reporterToken = await RegisterAndVerifyAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", reporterToken);
        var response = await _client.GetAsync("/api/admin/reports");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminToken_CannotAccessAReporterEndpoint()
    {
        await using (var db = _factory.CreateDbContext())
        {
            db.AdminUsers.Add(new AdminUser
            {
                Id = Guid.NewGuid(),
                FullName = "Test Admin",
                Email = "admin@cirs.gov.sl",
                PasswordHash = new BCryptPasswordHasher().Hash(Password),
                Role = AdminRole.SuperAdmin,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var login = await _client.PostAsJsonAsync("/api/admin/auth/login", new LoginRequest("admin@cirs.gov.sl", Password));
        var adminTokens = await login.Content.ReadFromJsonAsync<AuthTokenResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminTokens!.AccessToken);
        var response = await _client.GetAsync("/api/mobile/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithRememberMeTrue_IssuesALongLivedRefreshToken()
    {
        await RegisterAndVerifyAsync();

        var login = await _client.PostAsJsonAsync(
            "/api/mobile/auth/login", new ReporterLoginRequest(Email, Password, RememberMe: true));
        var tokens = await login.Content.ReadFromJsonAsync<ReporterAuthTokenResponse>();

        (tokens!.RefreshTokenExpiresAt - DateTimeOffset.UtcNow).Should().BeGreaterThan(TimeSpan.FromDays(29));
    }

    [Fact]
    public async Task Login_WithRememberMeFalse_IssuesAShortLivedRefreshToken()
    {
        await RegisterAndVerifyAsync();

        var login = await _client.PostAsJsonAsync(
            "/api/mobile/auth/login", new ReporterLoginRequest(Email, Password, RememberMe: false));
        var tokens = await login.Content.ReadFromJsonAsync<ReporterAuthTokenResponse>();

        // appsettings.Testing.json sets ReporterShortSessionHours to 1.
        (tokens!.RefreshTokenExpiresAt - DateTimeOffset.UtcNow).Should().BeLessThan(TimeSpan.FromHours(2));
    }

    [Fact]
    public async Task Refresh_PreservesTheOriginalRememberMeCategoryAcrossRotation()
    {
        await RegisterAndVerifyAsync();
        var login = await _client.PostAsJsonAsync(
            "/api/mobile/auth/login", new ReporterLoginRequest(Email, Password, RememberMe: false));
        var tokens = await login.Content.ReadFromJsonAsync<ReporterAuthTokenResponse>();

        var refreshed = await _client.PostAsJsonAsync(
            "/api/mobile/auth/refresh", new ReporterRefreshTokenRequest(tokens!.RefreshToken));
        var refreshedTokens = await refreshed.Content.ReadFromJsonAsync<ReporterAuthTokenResponse>();

        (refreshedTokens!.RefreshTokenExpiresAt - DateTimeOffset.UtcNow).Should().BeLessThan(TimeSpan.FromHours(2),
            "a short session must not silently become long-lived just by refreshing");
    }

    [Fact]
    public async Task RecordConsent_WithAValidReporterToken_PersistsEachGrant()
    {
        var accessToken = await RegisterAndVerifyAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new RecordConsentRequest([
            new ConsentGrantItem(ConsentType.Location, true, "v1"),
            new ConsentGrantItem(ConsentType.Camera, true, "v1"),
            new ConsentGrantItem(ConsentType.Notifications, false, "v1"),
            new ConsentGrantItem(ConsentType.DataProcessing, true, "v1")
        ]);

        var response = await _client.PostAsJsonAsync("/api/mobile/auth/consent", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var recorded = await response.Content.ReadFromJsonAsync<List<ConsentDto>>(JsonOptions);
        recorded.Should().HaveCount(4);
        recorded!.Single(c => c.ConsentType == ConsentType.Notifications).Granted.Should().BeFalse();
    }

    [Fact]
    public async Task RecordConsent_WithoutAToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/mobile/auth/consent",
            new RecordConsentRequest([new ConsentGrantItem(ConsentType.Location, true, "v1")]));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
