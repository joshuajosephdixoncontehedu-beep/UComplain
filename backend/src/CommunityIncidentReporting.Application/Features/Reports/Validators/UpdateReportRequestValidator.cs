using CommunityIncidentReporting.Application.Features.Reports.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.Reports.Validators;

public class UpdateReportRequestValidator : AbstractValidator<UpdateReportRequest>
{
    public UpdateReportRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.LocationDescription).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
    }
}
