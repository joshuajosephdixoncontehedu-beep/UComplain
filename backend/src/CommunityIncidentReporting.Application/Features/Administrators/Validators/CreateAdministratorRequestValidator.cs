using CommunityIncidentReporting.Application.Features.Administrators.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.Administrators.Validators;

public class CreateAdministratorRequestValidator : AbstractValidator<CreateAdministratorRequest>
{
    public CreateAdministratorRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.TemporaryPassword).NotEmpty().MinimumLength(10).MaximumLength(200);
    }
}
