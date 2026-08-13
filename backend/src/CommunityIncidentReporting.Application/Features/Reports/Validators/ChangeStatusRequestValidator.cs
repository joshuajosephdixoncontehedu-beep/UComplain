using CommunityIncidentReporting.Application.Features.Reports.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.Reports.Validators;

public class ChangeStatusRequestValidator : AbstractValidator<ChangeStatusRequest>
{
    public ChangeStatusRequestValidator()
    {
        RuleFor(x => x.NewStatus).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
