using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.MobileAuth.Validators;

public class RecordConsentRequestValidator : AbstractValidator<RecordConsentRequest>
{
    public RecordConsentRequestValidator()
    {
        RuleFor(x => x.Consents).NotEmpty();

        RuleForEach(x => x.Consents).ChildRules(item =>
        {
            item.RuleFor(x => x.ConsentType).IsInEnum();
            item.RuleFor(x => x.PolicyVersion).NotEmpty().MaximumLength(40);
        });
    }
}
