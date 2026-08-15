using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.MobileReports.Validators;

public class UpdateDraftRequestValidator : AbstractValidator<UpdateDraftRequest>
{
    public UpdateDraftRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty).When(x => x.CategoryId is not null);
        RuleFor(x => x.Description).MaximumLength(4000).When(x => x.Description is not null);
        RuleFor(x => x.LocationDescription).MaximumLength(300).When(x => x.LocationDescription is not null);
        RuleFor(x => x.Landmark).MaximumLength(300).When(x => x.Landmark is not null);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude is not null);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude is not null);
    }
}
