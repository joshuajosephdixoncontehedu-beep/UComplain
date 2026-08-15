using System.Net;

namespace CommunityIncidentReporting.Infrastructure.Email;

/// <summary>
/// Plain, dependency-free HTML + text builders for the transactional emails this system
/// sends. Deliberately simple (no external templating engine) — three short messages
/// don't justify one.
/// </summary>
public static class EmailTemplates
{
    private const string FooterText = "UComplain — Community Incident Reporting";

    public static (string Subject, string Html, string Text) EmailVerificationOtp(string fullName, string code, int expiryMinutes)
    {
        var subject = "Verify your email — UComplain";
        var greeting = WebUtility.HtmlEncode(fullName);
        var html = Wrap($"""
            <p>Hi {greeting},</p>
            <p>Use the code below to verify your email address. It expires in {expiryMinutes} minutes.</p>
            {CodeBlock(code)}
            <p>If you didn't request this, you can safely ignore this email.</p>
            """);
        var text = $"Hi {fullName},\n\nYour UComplain verification code is: {code}\nIt expires in {expiryMinutes} minutes.\n\nIf you didn't request this, you can safely ignore this email.\n\n{FooterText}";
        return (subject, html, text);
    }

    public static (string Subject, string Html, string Text) PasswordResetOtp(string fullName, string code, int expiryMinutes)
    {
        var subject = "Reset your password — UComplain";
        var greeting = WebUtility.HtmlEncode(fullName);
        var html = Wrap($"""
            <p>Hi {greeting},</p>
            <p>Use the code below to reset your password. It expires in {expiryMinutes} minutes.</p>
            {CodeBlock(code)}
            <p>If you didn't request a password reset, you can safely ignore this email — your password will not be changed.</p>
            """);
        var text = $"Hi {fullName},\n\nYour UComplain password reset code is: {code}\nIt expires in {expiryMinutes} minutes.\n\nIf you didn't request this, you can safely ignore this email.\n\n{FooterText}";
        return (subject, html, text);
    }

    public static (string Subject, string Html, string Text) Welcome(string fullName)
    {
        var subject = "Welcome to UComplain";
        var greeting = WebUtility.HtmlEncode(fullName);
        var html = Wrap($"""
            <p>Hi {greeting},</p>
            <p>Your email is verified and your UComplain account is ready. You can now report
            community incidents, attach photos or other media, and track their status from the
            mobile app.</p>
            """);
        var text = $"Hi {fullName},\n\nYour email is verified and your UComplain account is ready. You can now report community incidents, attach photos or other media, and track their status from the mobile app.\n\n{FooterText}";
        return (subject, html, text);
    }

    private static string CodeBlock(string code) =>
        $"""<p style="font-size:28px;font-weight:bold;letter-spacing:6px;margin:24px 0;">{WebUtility.HtmlEncode(code)}</p>""";

    private static string Wrap(string bodyHtml) => $"""
        <!doctype html>
        <html>
        <body style="font-family:Arial,Helvetica,sans-serif;color:#1a1a1a;max-width:480px;margin:0 auto;padding:24px;">
            {bodyHtml}
            <p style="color:#6b7280;font-size:12px;margin-top:32px;">{FooterText}</p>
        </body>
        </html>
        """;
}
