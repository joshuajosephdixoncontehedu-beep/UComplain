using CommunityIncidentReporting.Application.Features.Verification.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.Verification.Validators;

public class VerificationDecisionRequestValidator : AbstractValidator<VerificationDecisionRequest>
{
    public VerificationDecisionRequestValidator()
    {
        RuleFor(x => x.Action).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(2000);
        RuleFor(x => x.Reason)
            .NotEmpty()
            .When(x => x.Action is VerificationDecisionAction.Reject or VerificationDecisionAction.RequestClarification
                or VerificationDecisionAction.MarkDuplicate or VerificationDecisionAction.Escalate)
            .WithMessage("A reason is required for this verification decision.");
    }
}
