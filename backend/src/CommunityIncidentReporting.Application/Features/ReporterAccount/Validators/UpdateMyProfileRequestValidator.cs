using CommunityIncidentReporting.Application.Features.ReporterAccount.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.ReporterAccount.Validators;

public class UpdateMyProfileRequestValidator : AbstractValidator<UpdateMyProfileRequest>
{
    public UpdateMyProfileRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LanguagePreference).MaximumLength(16).When(x => x.LanguagePreference is not null);
    }
}
