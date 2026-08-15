using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.MobileReports.Validators;

public class SubmitDraftRequestValidator : AbstractValidator<SubmitDraftRequest>
{
    public SubmitDraftRequestValidator()
    {
        RuleFor(x => x.TruthDeclarationAccepted)
            .Equal(true)
            .WithMessage("You must confirm the truth declaration before submitting.");
    }
}
