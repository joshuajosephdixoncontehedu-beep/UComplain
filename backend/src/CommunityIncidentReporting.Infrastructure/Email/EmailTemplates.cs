using System.Net;

namespace CommunityIncidentReporting.Infrastructure.Email;

/// <summary>
/// Plain, dependency-free HTML + text builders for the transactional emails this system
/// sends. Deliberately simple (no external templating engine) — three short messages
/// don't justify one. HTML uses a table-based layout with fully inline styles (no
/// &lt;style&gt; block) — the safest pattern across email clients that strip &lt;head&gt;
/// styles or ignore flexbox/grid (notably Outlook desktop). The logo is read directly from
/// wwwroot/images/ and embedded as a data URI, computed once at class load — this used to
/// build a URL against APP_BASE_URL instead, but that env var is easy to leave unset on a
/// fresh deploy (it isn't in render.yaml's tracked vars), which silently produced a bare
/// "/images/..." path with no host — meaningless outside a browser, so every email client
/// just showed a broken-image placeholder. A data URI has no such dependency. The
/// tradeoff: Outlook desktop strips data: image sources, so the logo won't render there —
/// acceptable since every other client (Gmail, Apple Mail, Outlook web/mobile) shows it.
/// </summary>
public static class EmailTemplates
{
    private const string FooterText = "UComplain — Community Incident Reporting";

    // Matches the frontend's brand palette (frontend/src/app/globals.css).
    private const string NavyColor = "#0f1f3d";
    private const string CodeBgColor = "#eff6ff";
    private const string CodeBorderColor = "#bfdbfe";
    private const string CodeTextColor = "#1d4ed8";
    private const string BodyTextColor = "#1a1a1a";
    private const string MutedTextColor = "#94a3b8";
    private const string PageBgColor = "#eef2f7";
    private const string BorderColor = "#e2e8f0";
    private const string FooterBgColor = "#f8fafc";

    private static readonly Lazy<string?> LogoDataUri = new(BuildLogoDataUri);

    private static string? BuildLogoDataUri()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "wwwroot", "images", "ucomplain-logo.png");
            var bytes = File.ReadAllBytes(path);
            return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
        }
        catch (IOException)
        {
            // Falls back to no logo (see Wrap) rather than crashing email send over a missing asset.
            return null;
        }
    }

    public static (string Subject, string Html, string Text) EmailVerificationOtp(
        string fullName, string code, int expiryMinutes)
    {
        var subject = "Verify your email — UComplain";
        var greeting = WebUtility.HtmlEncode(fullName);
        var html = Wrap("Verify your email", $"""
            <p style="margin:0 0 16px;">Hi {greeting},</p>
            <p style="margin:0 0 8px;">Use the code below to verify your email address. It expires in
            <strong>{expiryMinutes} minutes</strong>.</p>
            {CodeBlock(code)}
            <p style="margin:16px 0 0;color:{MutedTextColor};">If you didn't request this, you can safely ignore this email.</p>
            """);
        var text = $"Hi {fullName},\n\nYour UComplain verification code is: {code}\nIt expires in {expiryMinutes} minutes.\n\nIf you didn't request this, you can safely ignore this email.\n\n{FooterText}";
        return (subject, html, text);
    }

    public static (string Subject, string Html, string Text) PasswordResetOtp(
        string fullName, string code, int expiryMinutes)
    {
        var subject = "Reset your password — UComplain";
        var greeting = WebUtility.HtmlEncode(fullName);
        var html = Wrap("Reset your password", $"""
            <p style="margin:0 0 16px;">Hi {greeting},</p>
            <p style="margin:0 0 8px;">Use the code below to reset your password. It expires in
            <strong>{expiryMinutes} minutes</strong>.</p>
            {CodeBlock(code)}
            <p style="margin:16px 0 0;color:{MutedTextColor};">If you didn't request a password reset, you can safely
            ignore this email — your password will not be changed.</p>
            """);
        var text = $"Hi {fullName},\n\nYour UComplain password reset code is: {code}\nIt expires in {expiryMinutes} minutes.\n\nIf you didn't request this, you can safely ignore this email.\n\n{FooterText}";
        return (subject, html, text);
    }

    public static (string Subject, string Html, string Text) Welcome(string fullName)
    {
        var subject = "Welcome to UComplain";
        var greeting = WebUtility.HtmlEncode(fullName);
        var html = Wrap("Welcome to UComplain", $"""
            <p style="margin:0 0 16px;">Hi {greeting},</p>
            <p style="margin:0;">Your email is verified and your UComplain account is ready. You can now report
            community incidents, attach photos or other media, and track their status from the
            mobile app.</p>
            """);
        var text = $"Hi {fullName},\n\nYour email is verified and your UComplain account is ready. You can now report community incidents, attach photos or other media, and track their status from the mobile app.\n\n{FooterText}";
        return (subject, html, text);
    }

    private static string CodeBlock(string code) => $"""
        <table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:20px 0;">
            <tr>
                <td style="background-color:{CodeBgColor};border:1px solid {CodeBorderColor};border-radius:8px;padding:16px 28px;text-align:center;">
                    <span style="font-family:'Courier New',Courier,monospace;font-size:32px;font-weight:bold;letter-spacing:10px;color:{CodeTextColor};">{WebUtility.HtmlEncode(code)}</span>
                </td>
            </tr>
        </table>
        """;

    private static string Wrap(string preheader, string bodyHtml)
    {
        var logoCell = LogoDataUri.Value is { } logoDataUri
            ? $"""<img src="{logoDataUri}" width="28" height="28" alt="UComplain" style="display:block;margin:4px;border:0;outline:none;" />"""
            : "";

        return $"""
            <!doctype html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>{WebUtility.HtmlEncode(preheader)}</title>
            </head>
            <body style="margin:0;padding:0;background-color:{PageBgColor};font-family:Arial,Helvetica,sans-serif;">
                <!-- Preheader: hidden preview text shown next to the subject line in most inboxes -->
                <div style="display:none;max-height:0;overflow:hidden;opacity:0;">{WebUtility.HtmlEncode(preheader)}</div>

                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:{PageBgColor};">
                    <tr>
                        <td align="center" style="padding:32px 16px;">
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:480px;background-color:#ffffff;border-radius:12px;border:1px solid {BorderColor};overflow:hidden;">
                                <tr>
                                    <td style="background-color:{NavyColor};padding:20px 28px;">
                                        <table role="presentation" cellpadding="0" cellspacing="0" border="0">
                                            <tr>
                                                <td style="background-color:#ffffff;border-radius:8px;width:36px;height:36px;text-align:center;vertical-align:middle;">
                                                    {logoCell}
                                                </td>
                                                <td style="padding-left:12px;color:#ffffff;font-size:17px;font-weight:bold;font-family:Arial,Helvetica,sans-serif;">
                                                    UComplain
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:32px 28px;color:{BodyTextColor};font-size:15px;line-height:1.6;">
                                        {bodyHtml}
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding:16px 28px;background-color:{FooterBgColor};border-top:1px solid {BorderColor};color:{MutedTextColor};font-size:12px;">
                                        {FooterText}
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }
}
