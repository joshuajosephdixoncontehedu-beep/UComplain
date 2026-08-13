using CommunityIncidentReporting.Application.Features.Settings.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.Settings.Validators;

public class UpdateSettingsRequestValidator : AbstractValidator<UpdateSettingsRequest>
{
    public UpdateSettingsRequestValidator()
    {
        RuleFor(x => x.OrganizationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OrganizationContactEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.DefaultVerificationSlaHours).InclusiveBetween(1, 24 * 30);
        RuleFor(x => x.DuplicateDetectionWindowHours).InclusiveBetween(1, 24 * 30);
        RuleFor(x => x.ReporterDataRetentionMonths).InclusiveBetween(1, 120);
        RuleFor(x => x.AuditLogRetentionMonths).InclusiveBetween(1, 120);
        RuleFor(x => x.WhatsAppPlaceholderNote).MaximumLength(1000);
    }
}
