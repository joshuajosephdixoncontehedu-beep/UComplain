using System.Security.Claims;
using CommunityIncidentReporting.Api.Authorization;
using CommunityIncidentReporting.Application.Features.MobileAuth;
using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CommunityIncidentReporting.Api.Controllers.Mobile;

/// <summary>
/// Mobile reporter self-service auth — entirely separate from api/admin/auth. Every
/// anonymous endpoint here is rate-limited (see Program.cs's "auth"/"otp" limiter
/// policies) since they're the highest-value targets for credential stuffing/OTP
/// brute-forcing in this API.
/// </summary>
[ApiController]
[Route("api/mobile/auth")]
public class MobileAuthController(IReporterAuthService reporterAuthService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<RegisterReporterResponse>> Register(
        [FromBody] RegisterReporterRequest request, CancellationToken cancellationToken)
    {
        var response = await reporterAuthService.RegisterAsync(request, RemoteIp(), UserAgent(), cancellationToken);
        return Ok(response);
    }

    [HttpPost("verify-email-otp")]
    [AllowAnonymous]
    [EnableRateLimiting("otp")]
    public async Task<ActionResult<ReporterAuthTokenResponse>> VerifyEmailOtp(
        [FromBody] VerifyEmailOtpRequest request, CancellationToken cancellationToken)
    {
        var response = await reporterAuthService.VerifyEmailOtpAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("resend-email-otp")]
    [AllowAnonymous]
    [EnableRateLimiting("otp")]
    public async Task<ActionResult<GenericMessageResponse>> ResendEmailOtp(
        [FromBody] ResendEmailOtpRequest request, CancellationToken cancellationToken)
    {
        await reporterAuthService.ResendEmailOtpAsync(request, RemoteIp(), UserAgent(), cancellationToken);
        return Ok(new GenericMessageResponse("If an account is pending verification for that email, a new code has been sent."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<ReporterAuthTokenResponse>> Login(
        [FromBody] ReporterLoginRequest request, CancellationToken cancellationToken)
    {
        var response = await reporterAuthService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ReporterAuthTokenResponse>> Refresh(
        [FromBody] ReporterRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await reporterAuthService.RefreshAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize(Policy = Policies.RequireReporter)]
    public async Task<IActionResult> Logout(
        [FromBody] ReporterRefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await reporterAuthService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize(Policy = Policies.RequireReporter)]
    public async Task<ActionResult<ReporterProfileDto>> Me(CancellationToken cancellationToken)
    {
        var reporterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await reporterAuthService.GetProfileAsync(reporterId, cancellationToken);
        return Ok(profile);
    }

    [HttpPost("consent")]
    [Authorize(Policy = Policies.RequireReporter)]
    public async Task<ActionResult<IReadOnlyList<ConsentDto>>> RecordConsent(
        [FromBody] RecordConsentRequest request, CancellationToken cancellationToken)
    {
        var reporterId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var response = await reporterAuthService.RecordConsentAsync(reporterId, request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("otp")]
    public async Task<ActionResult<GenericMessageResponse>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await reporterAuthService.ForgotPasswordAsync(request, RemoteIp(), UserAgent(), cancellationToken);
        return Ok(new GenericMessageResponse("If that email is registered, a password reset code has been sent."));
    }

    [HttpPost("verify-password-reset-otp")]
    [AllowAnonymous]
    [EnableRateLimiting("otp")]
    public async Task<ActionResult<GenericMessageResponse>> VerifyPasswordResetOtp(
        [FromBody] VerifyPasswordResetOtpRequest request, CancellationToken cancellationToken)
    {
        await reporterAuthService.VerifyPasswordResetOtpAsync(request, cancellationToken);
        return Ok(new GenericMessageResponse("Code verified. You may now set a new password."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<GenericMessageResponse>> ResetPassword(
        [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await reporterAuthService.ResetPasswordAsync(request, cancellationToken);
        return Ok(new GenericMessageResponse("Your password has been reset. Please log in with your new password."));
    }

    private string? RemoteIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent() => Request.Headers.UserAgent.ToString() is { Length: > 0 } ua ? ua : null;
}
