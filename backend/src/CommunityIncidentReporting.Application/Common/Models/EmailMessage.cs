namespace CommunityIncidentReporting.Application.Common.Models;

public record EmailMessage(string ToEmail, string Subject, string HtmlBody, string TextBody);
