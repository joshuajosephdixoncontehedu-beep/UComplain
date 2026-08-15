using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.MobileReports.Validators;

public class CreateMobileReportRequestValidator : AbstractValidator<CreateMobileReportRequest>
{
    public CreateMobileReportRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.IncidentOccurredAt).NotEmpty();
        RuleFor(x => x.LocationDescription).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude is not null);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude is not null);
        RuleFor(x => x.Landmark).MaximumLength(300).When(x => x.Landmark is not null);
    }
}
