using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.MobileReports.Validators;

public class AddReportInformationRequestValidator : AbstractValidator<AddReportInformationRequest>
{
    public AddReportInformationRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.AttachmentId).NotEqual(Guid.Empty).When(x => x.AttachmentId is not null);
    }
}
